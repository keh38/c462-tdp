function R = analyzePatternTapping(jsonFile)
% ANALYZEPATTERNTAPPING -- folded (pattern-repeat) analysis of a tapping recording
%
%   R = analyzePatternTapping(jsonFile)
%
%  jsonFile is the path to a serialized TappingTrial (see TappingTrial.cs). The
%  matching TapStreamer recording is the same path with .json -> .wav:
%     channel 1 = tap sensor
%     channel 2 = pattern-element onset pulses (1 kHz loopback fiducials)
%
%  The trial's PacerPattern field is the sequence of intervals tiled to build
%  the repeating pacer pattern. Its LENGTH, N, is the number of stimulus
%  elements per repeat. BOTH event trains are folded the same way, purely by
%  COUNT: repeat r starts at the (r-1)*N+1-th fiducial pulse, and any event --
%  pulse or tap -- is placed at its own time minus that repeat's first-pulse
%  time. There is no tap<->pulse matching: a tap's within-repeat x is
%  tapTime - repeatStart, exactly as a pulse's is pulseTime - repeatStart.
%  Reference lines drawn from the interval values mark where the pulses sit
%  within a repeat; a horizontal line marks the repeat at which the distractor
%  turns on (LeadIn / sum(PacerPattern) repeats).
%
%  Figure:
%     bottom    : full waveform, tap and pulse overplotted (both normalized to
%                 the peak tap amplitude), light vertical onset markers
%     top-left  : tap-template summary (unchanged)
%     top-right : folded tap raster, one row per repeat, time increasing upward

%% ---- 0. Arguments ----------------------------------------------------------
arguments
    jsonFile {mustBeTextScalar}
end
jsonFile = char(jsonFile);

%% ---- 1. Load trial, derive and check the wav path --------------------------
s = jsondecode(fileread(jsonFile));
trial = s.trial;

pacerPattern = double(trial.PacerPattern(:));
N = numel(pacerPattern);
if N < 1
    error('analyzePatternTapping:pattern', ...
        'PacerPattern is empty; cannot determine the number of elements per repeat.');
end

[pth, stem] = fileparts(jsonFile);
wavFile = fullfile(pth, [stem '.wav']);
if ~isfile(wavFile)
    error('analyzePatternTapping:wav', ...
        'No matching .wav found next to the .json: %s', wavFile);
end

%% ---- 2. Read wav, split channels ------------------------------------------
[X, fs] = audioread(wavFile);
if size(X, 2) < 2
    error('analyzePatternTapping:channels', ...
        'Expected a 2-channel wav (ch1 = tap, ch2 = pulse); got %d channel(s).', size(X,2));
end
tap   = X(:, 1);
pulse = X(:, 2);

%% ---- 3. Detect event trains (reuse existing analyzers) ---------------------
pulseTimes = hts.detectAudioPulses(pulse, 'Fs', fs);
pulseTimes = sort(pulseTimes(:));

[tapTime, tapAmplitude, template] = hts.analyzeTaps(tap, fs);
tapTime = sort(tapTime(:));

nP = numel(pulseTimes);
if nP < 1
    error('analyzePatternTapping:pulses', 'No fiducial pulses detected on channel 2.');
end

%% ---- 4. Fold BOTH trains by pulse count (no tap<->pulse matching) ----------
% Repeat r is defined purely by count: it starts at the (r-1)*N+1-th fiducial.
% Its origin is that pulse's measured time. A pulse or a tap is folded by
% subtracting the origin of the repeat it falls in -- the same operation for
% both, so the taps drift relative to the pulses only if they truly do.
firstPulseIdx   = (1:N:nP).';                     % first-pulse index of each repeat
cycleStartTimes = pulseTimes(firstPulseIdx);      % nCycles x 1 repeat origins
nCycles         = numel(cycleStartTimes);

% pulses, wrapped by their ordinal index
pulseRepeat = floor((0:nP-1).'/N) + 1;
pulseWrapX  = pulseTimes - cycleStartTimes(pulseRepeat);

% taps, wrapped the same way: time-binned into the repeat that contains them
edges     = [cycleStartTimes; inf];
tapRepeat = discretize(tapTime, edges);           % NaN for taps before the 1st pulse
valid     = ~isnan(tapRepeat);
tapWrapX  = nan(numel(tapTime), 1);
tapWrapX(valid) = tapTime(valid) - cycleStartTimes(tapRepeat(valid));

%% ---- 5. Nominal geometry from the interval values -------------------------
% Within-repeat pulse positions + period, with ms/s auto-detected by matching
% sum(pattern) to the MEASURED repeat period so reference lines land correctly.
sumInt        = sum(pacerPattern);
repeatPeriods = diff(cycleStartTimes);            % measured, one per repeat gap
if ~isempty(repeatPeriods)
    measPeriod = median(repeatPeriods);
elseif nP >= 2
    measPeriod = median(diff(pulseTimes)) * N;
else
    measPeriod = NaN;
end

cands = [1, 1e-3];                                % s, ms
if isfinite(measPeriod) && sumInt > 0
    [~, ki]   = min(abs(sumInt*cands - measPeriod));
    unitScale = cands(ki);
else
    unitScale = 1e-3;                             % lab convention: intervals in ms
end
refPos        = unitScale * [0; cumsum(pacerPattern(1:end-1))];   % N within-repeat positions
patternPeriod = unitScale * sumInt;

% LeadIn is an integer number of repeats; LeadIn / sum(PacerPattern) is a
% unit-independent ratio, so no unit conversion is needed here.
if isfield(trial, 'LeadIn') && sumInt > 0
    leadCycles = round(double(trial.LeadIn) / sumInt);
else
    leadCycles = 0;
end

%% ---- 6. Drift diagnostic ---------------------------------------------------
% Count-based folding is only valid if every repeat really holds exactly N
% fiducials. If the repeat period is unstable, or its median doesn't match the
% nominal period, the fold will diagonal even with perfectly locked tapping --
% the usual cause being that the fiducials-per-repeat isn't N (extra onsets
% from another stream, or missed/spurious pulse detections).
if numel(repeatPeriods) >= 2
    cvPeriod = std(repeatPeriods) / mean(repeatPeriods);
else
    cvPeriod = NaN;
end

fprintf(['%d pulses (%d repeats x N=%d), %d taps folded. ', ...
         'Nominal period %.3f s (intervals read as %s); ', ...
         'measured %.3f s (CV %.1f%%); distractor on at repeat %d.\n'], ...
    nP, nCycles, N, nnz(valid), patternPeriod, ternary(unitScale==1e-3,'ms','s'), ...
    measPeriod, 100*cvPeriod, leadCycles+1);

if isfinite(cvPeriod) && cvPeriod > 0.02
    warning('analyzePatternTapping:periodUnstable', ...
        ['Repeat period varies (CV = %.1f%%). Count-based wrapping assumes exactly ', ...
         'N=%d fiducials per repeat -- check for extra or missed pulses.'], 100*cvPeriod, N);
end
if isfinite(measPeriod) && patternPeriod > 0 && abs(measPeriod/patternPeriod - 1) > 0.05
    warning('analyzePatternTapping:periodMismatch', ...
        ['Measured repeat period (%.3f s) differs from nominal (%.3f s) by %.0f%%. ', ...
         'The N=%d count likely does not match the fiducials per repeat.'], ...
        measPeriod, patternPeriod, 100*(measPeriod/patternPeriod - 1), N);
end

%% ---- 7. Package results ----------------------------------------------------
R = struct();
R.fs                = fs;
R.tap               = tap;
R.pulse             = pulse;
R.pulseTimes        = pulseTimes;
R.tapTime           = tapTime;
R.tapAmplitude      = tapAmplitude;
R.template          = template;
R.patternLength     = N;                    % elements per repeat
R.cycleStartTimes   = cycleStartTimes;      % nCycles x 1 repeat origins
R.nCycles           = nCycles;
R.pulseRepeat       = pulseRepeat;          % nP x 1
R.pulseWrapX        = pulseWrapX;           % nP x 1, pulse time re repeat onset (s)
R.tapRepeat         = tapRepeat;            % nTap x 1 (NaN before first pulse)
R.tapWrapX          = tapWrapX;             % nTap x 1, tap time re repeat onset (s)
R.refPos            = refPos;               % within-repeat pulse positions (s)
R.patternPeriod     = patternPeriod;        % one repeat (s)
R.measPeriod        = measPeriod;           % measured repeat period (s)
R.periodCV          = cvPeriod;             % repeat-period coefficient of variation
R.leadCycles        = leadCycles;           % pacer-only repeats before distractor
R.unitScale         = unitScale;            % 1 => intervals in s, 1e-3 => ms

%% ---- 8. Figure -------------------------------------------------------------
buildFigure(R, stem);
end


% ============================================================================
function buildFigure(R, name)

fig = figure('Name', 'Pattern tapping analysis', 'Color', 'w');
try
    fig.Position(3:4) = [1100 800];
    movegui(fig, 'onscreen');
catch
end

% 4 rows: template | raster occupy rows 1-3; the waveform strip occupies row 4.
tOuter = tiledlayout(fig, 4, 2, 'TileSpacing', 'compact', 'Padding', 'compact');

axTL = nexttile(tOuter, 1, [3 1]);           % top-left: template (unchanged)
hts.plotTapTemplate(axTL, R.template);

axRas = nexttile(tOuter, 2, [3 1]);          % top-right: folded raster
drawRaster(axRas, R);

axW = nexttile(tOuter, 7, [1 2]);            % bottom: waveform strip
drawWaveform(axW, R);

title(tOuter, strrep(name, '_', '\_'), 'FontWeight', 'bold', 'Interpreter', 'tex');
end


% ============================================================================
function drawWaveform(ax, R)
% Tap and pulse overplotted on a single baseline, both normalized to the peak
% tap amplitude; light vertical lines mark detected onsets.

t = (0:numel(R.tap)-1).' / R.fs;
m = max(abs(R.tap)) + eps;                    % normalize BOTH channels to peak tap
tapN   = R.tap   / m;
pulseN = R.pulse / m;

ymax = max([1, max(abs(pulseN))]);
yl   = [-1.05*ymax, 1.05*ymax];

hold(ax, 'on');
hPO  = drawEventLines(ax, R.pulseTimes, yl, [0.70 0.80 1.00], '-');   % light blue
hTO  = drawEventLines(ax, R.tapTime,    yl, [1.00 0.80 0.70], '-');   % light orange
hTap = plot(ax, t, tapN,   'Color', [0.15 0.15 0.15]);
hPul = plot(ax, t, pulseN, 'Color', [0.35 0.35 0.55]);
hold(ax, 'off');

set([hPO hTO], 'LineWidth', 0.5);
ylim(ax, yl);
xlim(ax, [t(1) t(end)]);
xlabel(ax, 'time (s)');
ylabel(ax, 'norm. amplitude');
title(ax, 'Recording with detected pulses and taps');

hTap.DisplayName = 'tap';
hPul.DisplayName = 'pulse';
hPO.DisplayName  = 'pulse onset';
hTO.DisplayName  = 'tap onset';
legend(ax, [hTap hPul hPO hTO], 'Location', 'northeastoutside', 'Box', 'off');
end


% ============================================================================
function drawRaster(ax, R)
% One row per pattern repeat; every folded tap plotted at its time-re-repeat.
% Repeat number (time) increases upward. Light vertical lines mark nominal
% pulse positions; a horizontal line marks distractor onset.

nC  = R.nCycles;
ok  = ~isnan(R.tapWrapX);
xv  = R.tapWrapX(ok);
yv  = R.tapRepeat(ok);

% x-limits: a little negative headroom for anticipations, out to one period
if isempty(xv)
    xlo = -0.15*R.patternPeriod; xhi = R.patternPeriod;
else
    xlo = min([xv; -0.05*R.patternPeriod]);
    xhi = max([xv;  R.patternPeriod]);
end
pad = 0.05 * max(xhi - xlo, eps);
xl  = [xlo - pad, xhi + pad];

hold(ax, 'on');

% light reference lines at nominal within-repeat pulse positions + period edge
for k = 1:numel(R.refPos)
    xline(ax, R.refPos(k), 'Color', [0.85 0.55 0.25], 'LineWidth', 1);
end
xline(ax, R.patternPeriod, 'Color', [0.85 0.55 0.25], 'LineWidth', 1, 'LineStyle', ':');

% horizontal line where the distractor turns on (a repeat boundary)
yLead = R.leadCycles + 0.5;
if yLead > 0.5 && yLead < nC + 0.5
    yline(ax, yLead, 'Color', [0.85 0.55 0.25], 'LineWidth', 1.25, 'LineStyle', '--');
    text(ax, xl(2), yLead, ' distractor on ', 'Color', [0.70 0.42 0.18], ...
        'VerticalAlignment', 'bottom', 'HorizontalAlignment', 'right', 'FontSize', 8);
end

% folded taps
scatter(ax, xv, yv, 28, 's', 'filled', 'MarkerFaceColor', [0.15 0.15 0.15]);

hold(ax, 'off');

ylim(ax, [0.5, nC + 0.5]);        % default YDir => repeat 1 at bottom, time upward
xlim(ax, xl);
xlabel(ax, 'tap time re: repeat onset (s)');
ylabel(ax, 'pattern repeat #');
title(ax, 'Tap raster (folded by pattern repeat)');
grid(ax, 'on');
end


% ============================================================================
function h = drawEventLines(ax, times, yspan, colr, style)
% Draw a set of vertical lines as a single NaN-separated line object.
times = times(:).';
if isempty(times)
    h = plot(ax, NaN, NaN, style, 'Color', colr);
    return;
end
x = reshape([times; times; nan(1, numel(times))], [], 1);
y = repmat([yspan(1); yspan(2); NaN], numel(times), 1);
h = plot(ax, x, y, style, 'Color', colr);
end


% ============================================================================
function out = ternary(cond, a, b)
if cond, out = a; else, out = b; end
end
