function R = analyzePatternTapping(jsonFile, options)
% ANALYZEPATTERNTAPPING -- folded (pattern-repeat) analysis of a tapping recording
%
%   R = analyzePatternTapping(jsonFile)
%   R = analyzePatternTapping(jsonFile, Window_s=...)
%
%  jsonFile is the path to a serialized TappingTrial (see TappingTrial.cs). The
%  matching TapStreamer recording is the same path with .json -> .wav:
%     channel 1 = tap sensor
%     channel 2 = pattern-element onset pulses (1 kHz loopback fiducials)
%
%  The trial's PacerPattern field is the sequence of intervals that is tiled to
%  build the repeating pacer pattern. Its LENGTH, N, is the number of stimulus
%  elements per repeat. The fiducial pulse train is wrapped every N pulses -- by
%  COUNT, not by interval value -- so that time = 0 is the first pulse of each
%  repeat. Reference lines drawn from the interval values mark where the pulses
%  sit within a repeat; a horizontal line marks the repeat at which the
%  distractor turns on (LeadIn / sum(PacerPattern) repeats).
%
%  Figure:
%     bottom    : full waveform, tap and pulse overplotted (both normalized to
%                 the peak tap amplitude), light vertical onset markers, ~half
%                 the previous height
%     top-left  : tap-template summary (unchanged)
%     top-right : folded tap raster, one row per repeat, time increasing upward
%
%  Name-Value options:
%    Window_s - per-pulse match half-width in seconds; 0 => IPI/2 (default 0)

%% ---- 0. Arguments ----------------------------------------------------------
arguments
    jsonFile {mustBeTextScalar}
    options.Window_s (1,1) {mustBeNumeric} = 0     % half-width (s); 0 => IPI/2
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

%% ---- 4. Assign the nearest tap to each pulse (unchanged logic) -------------
% Symmetric window centred on the pulse; default half-width = IPI/2 tiles the
% timeline so each tap belongs to exactly one pulse and anticipatory (negative)
% asynchronies register naturally. The `used` guard keeps a tap from being
% claimed twice even if you widen Window_s past IPI/2.
IPI = median(diff(pulseTimes));
if isempty(IPI) || ~isfinite(IPI), IPI = 0; end

W = options.Window_s;
if W <= 0, W = IPI/2; end

latency = nan(nP, 1);
tapIdx  = nan(nP, 1);
used    = false(numel(tapTime), 1);
for i = 1:nP
    cand = find(tapTime >= pulseTimes(i) - W & ...
                tapTime <  pulseTimes(i) + W & ~used);
    if ~isempty(cand)
        [~, j] = min(abs(tapTime(cand) - pulseTimes(i)));   % nearest, not first
        sel = cand(j);
        latency(i) = tapTime(sel) - pulseTimes(i);
        tapIdx(i)  = sel;
        used(sel)  = true;
    end
end
responded = ~isnan(latency);

%% ---- 5. Fold the pulse train into pattern repeats (by count) ---------------
% Repeat r owns fiducial pulses (r-1)*N+1 .. r*N. time = 0 is that repeat's
% first pulse. Each responding tap is placed at (tapTime - repeatStart), so a
% tap anticipating a repeat's first pulse lands as a small negative value in
% the correct row rather than at the top of the previous row.
cycleRow   = floor((0:nP-1)'/N) + 1;               % nP x 1, repeat index per pulse
nCycles    = max([cycleRow; 1]);
firstIdx   = (cycleRow - 1)*N + 1;                 % first-pulse index of each repeat
cycleStart = pulseTimes(firstIdx);                 % nP x 1

cycleTapX = nan(nP, 1);
cycleTapX(responded) = tapTime(tapIdx(responded)) - cycleStart(responded);

% Nominal within-repeat pulse positions come from the interval values. Detect
% whether the intervals are in ms or s by matching sum(pattern) to the measured
% repeat period, so the reference lines land correctly regardless of unit and
% any real drift from the nominal spacing stays visible.
sumInt = sum(pacerPattern);
if nP > N
    measPeriod = median(pulseTimes(1+N:nP) - pulseTimes(1:nP-N));
elseif nP >= 2
    measPeriod = median(diff(pulseTimes)) * N;
else
    measPeriod = NaN;
end
unitScale = 1e-3;                               % lab convention: intervals in ms
refPos        = unitScale * [0; cumsum(pacerPattern(1:end-1))];   % N within-repeat positions
patternPeriod = unitScale * sumInt;

% LeadIn is an integer number of repeats; LeadIn / sum(PacerPattern) is a
% unit-independent ratio, so no unit conversion is needed here.
if isfield(trial, 'LeadIn') && sumInt > 0
    leadCycles = round(double(trial.LeadIn) / sumInt);
else
    leadCycles = 0;
end

fprintf(['%d pulses, %d taps, %d responded (%.0f%%). ', ...
         'Pattern: %d elements/repeat, %d repeats; ', ...
         'nominal period %.3f s (intervals read as %s), measured %.3f s; ', ...
         'distractor on at repeat %d.\n'], ...
    nP, numel(tapTime), nnz(responded), 100*mean(responded), ...
    N, nCycles, patternPeriod, ternary(unitScale==1e-3, 'ms', 's'), measPeriod, leadCycles+1);

%% ---- 6. Package results ----------------------------------------------------
R = struct();
R.fs                = fs;
R.tap               = tap;
R.pulse             = pulse;
R.pulseTimes        = pulseTimes;
R.tapTime           = tapTime;
R.tapAmplitude      = tapAmplitude;
R.latency           = latency;              % nP x 1, tap re pulse (NaN = no response)
R.tapIndexForPulse  = tapIdx;               % nP x 1, index into tapTime
R.template          = template;
R.patternLength     = N;                    % elements per repeat
R.cycleRow          = cycleRow;             % nP x 1, repeat index per pulse
R.cycleTapX         = cycleTapX;            % nP x 1, tap time re repeat onset (NaN = none)
R.nCycles           = nCycles;
R.refPos            = refPos;               % within-repeat pulse positions (s)
R.patternPeriod     = patternPeriod;        % one repeat (s)
R.leadCycles        = leadCycles;           % pacer-only repeats before distractor
R.unitScale         = unitScale;            % 1 => intervals in s, 1e-3 => ms

%% ---- 7. Figure -------------------------------------------------------------
buildFigure(R, sprintf('%s: %s', stem, trial.Tag));
end


% ============================================================================
function buildFigure(R, name)

fig = figure('Name', 'Pattern tapping analysis', 'Color', 'w');
try
    fig.Position(3:4) = [1100 800];
    movegui(fig, 'onscreen');   % nudge fully back onto the display
catch
end
% 4 rows: template | raster occupy rows 1-3; the waveform strip occupies row 4
% (~1/4 of the figure = about half its previous height).
tOuter = tiledlayout(fig, 4, 2, 'TileSpacing', 'compact', 'Padding', 'compact');

axTL = nexttile(tOuter, 1, [3 1]);           % top-left: template (unchanged)
hts.plotTapTemplate(axTL, R.template);

axRas = nexttile(tOuter, 2, [3 1]);          % top-right: folded raster
drawRaster(axRas, R);

axW = nexttile(tOuter, 7, [1 2]);            % bottom: half-height waveform strip
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
% One row per pattern repeat; a dot at each responding tap's time-re-repeat.
% Repeat number (time) increases upward. Light vertical lines mark the nominal
% within-repeat pulse positions; a horizontal line marks distractor onset.

nC   = R.nCycles;
resp = ~isnan(R.cycleTapX);
xv   = R.cycleTapX(resp);
yv   = R.cycleRow(resp);

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

% responding taps
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
% Draw a set of vertical lines as a single line object (NaN-separated) so it
% carries one legend entry and does not rescale the axes.

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
