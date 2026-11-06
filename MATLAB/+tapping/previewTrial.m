function fig = previewTrial(trial)
%PREVIEWTRIAL  Preview of a single decoded tapping trial.
%
%   fig = tapping.previewTrial(trial)
%
%   TRIAL is one element of the struct array you get from
%       s = jsondecode(fileread('Tapping.Demo2.json'));   trial = s.Trials(k);
%
%   The preview is a FAIR-ENOUGH model of the HTS's reading of the file, not
%   ground truth. It answers "is this trial STRUCTURED the way I intended".
%   Timing truth lives in the recorded WAV + loopback fiducial, never here.
%
%   Panels:
%     1  Onsets in ABSOLUTE TIME - Pacer on top, Distractor below, each
%        labelled with the stimulus (A/B) in that role this trial. The
%        distractor lane shows its pre-delay decomposed into LeadIn (shared
%        with the pacer) and Offset (the distractor's additional phase).
%     2  Pacer interval (ms) vs element index - flat line + scatter = jitter.
%        For a drifted pacer (PacerDrift present) the panel also shades the
%        steady lead-in (grey) and each hold-and-ramp ramp (amber), draws the
%        RangeFrac bound (dotted) and base interval (dashed), and the title
%        summarises the drift (method, base, bound hits / ramp count).
%     (+) Distractor interval panel, only when DistractorDrift is present.
%     3+ One panel per ParameterProfile, values looped over elements (index).
%
%   NOTE: the top panel is time-space; panels 2+ are index-space, so only the
%   lower panels share an x-axis. Time-space packs tight for long trials.

% ---- normalise what jsondecode handed us --------------------------------
pacerIv = asRow(getField(trial, 'PacerIntervals', []));
distIv  = asRow(getField(trial, 'DistractorIntervals', []));
leadIn  = double(getField(trial, 'LeadIn', 0));
offset  = double(getField(trial, 'Offset', 0));
tag     = char(string(getField(trial, 'Tag', '')));
profiles = asProfileList(getField(trial, 'ParameterProfiles', {}));
pDrift  = asDrift(getField(trial, 'PacerDrift', []));
dDrift  = asDrift(getField(trial, 'DistractorDrift', []));

N = numel(pacerIv);
if N < 1
    error('previewTrial:emptyPacer', 'trial has no pacer intervals to plot');
end

% ---- resolve the A/B role binding for the lane labels -------------------
pacerLetter = upper(char(string(getField(trial, 'Pacer', 'A'))));
pacerLetter = pacerLetter(1);
if pacerLetter == 'A', distLetter = 'B'; else, distLetter = 'A'; end
pacerLabel = sprintf('Pacer (%c)', pacerLetter);
distLabel  = sprintf('Distractor (%c)', distLetter);

% ---- reconstruct onset times (HTS convention) ---------------------------
% The PACER starts at t = 0 (no lead-in silence). LeadIn and Offset both apply
% ONLY to the distractor and sum into its single start delay, so the
% DISTRACTOR's first onset is at LeadIn + Offset. LeadIn = "let the subject
% hear the pacer alone for a while first"; Offset = the phase once the
% distractor enters. Element i plays, then interval(i) is the gap after it; a
% silent interval follows the final pulse.
pacerOnset = [0, cumsum(pacerIv(1:end-1))];   % 1xN, element i onset (pacer at t=0)
totalDur   = sum(pacerIv);

dOn = [];
M = numel(distIv);
if M > 0
    t = leadIn + offset;  j = 0;
    while t < totalDur
        dOn(end+1) = t;                          %#ok<AGROW>
        t = t + distIv(mod(j, M) + 1);
        j = j + 1;
    end
end

% ---- time axis: seconds for long trials, ms for short -------------------
if totalDur >= 10000
    sc = 1e-3;  tUnit = 's';
else
    sc = 1;     tUnit = 'ms';
end

% ---- draw ----------------------------------------------------------------
fig = figure('Color', 'w');
nTiles = 2 + ~isempty(dDrift) + numel(profiles);
tl = tiledlayout(fig, nTiles, 1, 'TileSpacing', 'compact', 'Padding', 'compact');
if ~isempty(tag)
    title(tl, tag, 'Interpreter', 'none', 'FontWeight', 'normal');
end

cPacer = [0.15 0.45 0.75];   % blue
cDist  = [0.85 0.35 0.12];   % orange
cLead  = [0.60 0.60 0.60];   % grey  - LeadIn band
cOff   = [0.90 0.70 0.25];   % amber - Offset band
idxAxes = gobjects(1 + numel(profiles), 1);   % the index-space panels (2..end)

% ---- Panel 1: onset times (absolute) ------------------------------------
ax = nexttile(tl); hold(ax, 'on');
yP = 1; yD = 0; h = 0.32;

% LeadIn then Offset bands on the distractor lane: the pacer-only stretch,
% then the phase shift, running from t=0 up to the distractor's first onset
bandLo = -0.6; bandHi = yD + h;
if leadIn > 0
    drawSpan(ax, 0, leadIn*sc, bandLo, bandHi, cLead, 0.18);
    labelSpan(ax, 0, leadIn*sc, bandLo, sprintf('LeadIn %g ms', leadIn), cLead);
end
if offset ~= 0
    drawSpan(ax, leadIn*sc, (leadIn+offset)*sc, bandLo, bandHi, cOff, 0.22);
    labelSpan(ax, leadIn*sc, (leadIn+offset)*sc, bandLo, sprintf('Offset %g ms', offset), cOff*0.7);
end

plotTicks(ax, pacerOnset*sc, yP, h, cPacer);        % pacer at its onset times
if ~isempty(dOn)
    plotTicks(ax, dOn*sc, yD, h, cDist);            % distractor at its onset times
end

ylim(ax, [-0.6 1.6]);
yticks(ax, [0 1]); yticklabels(ax, {distLabel, pacerLabel});
xlim(ax, [0 totalDur*sc]);
xlabel(ax, sprintf('time (%s)', tUnit));
title(ax, 'onsets', 'FontWeight', 'normal');
hold(ax, 'off');

% ---- Panel 2: pacer interval values (the jitter view, index-space) ------
ax = nexttile(tl); idxAxes(1) = ax; hold(ax, 'on');
ttl = 'pacer interval (ms)';
if ~isempty(pDrift)                     % drift: lead-in, ramps, bounds, base line
    ttl = sprintf('%s - %s', ttl, driftOverlay(ax, pDrift, pacerIv));
end
plot(ax, 1:N, pacerIv, '-o', 'Color', cPacer, ...
     'MarkerFaceColor', cPacer, 'MarkerSize', 4, 'LineWidth', 1);
if isempty(pDrift)
    yline(ax, mean(pacerIv), '--', 'Color', [cPacer 0.6]);
end
ylabel(ax, 'ms');
title(ax, ttl, 'FontWeight', 'normal', 'Interpreter', 'none');
hold(ax, 'off');

% ---- Drifted distractor: its own interval panel (its own element index) --
if ~isempty(dDrift)
    ax = nexttile(tl); hold(ax, 'on');
    nD = numel(distIv);
    ttl = sprintf('distractor interval (ms) - %s', driftOverlay(ax, dDrift, distIv));
    plot(ax, 1:nD, distIv, '-o', 'Color', cDist, ...
         'MarkerFaceColor', cDist, 'MarkerSize', 4, 'LineWidth', 1);
    xlim(ax, [0.5 nD + 0.5]);
    xlabel(ax, 'distractor element index (before looping)');
    ylabel(ax, 'ms');
    title(ax, ttl, 'FontWeight', 'normal', 'Interpreter', 'none');
    hold(ax, 'off');
end

% ---- Panels 3+: parameter profiles (index-space) ------------------------
for p = 1:numel(profiles)
    ax = nexttile(tl); idxAxes(1 + p) = ax;
    vals = asRow(getField(profiles{p}, 'Values', []));
    item = char(string(getField(profiles{p}, 'Item', sprintf('profile %d', p))));
    if isempty(vals)
        title(ax, sprintf('%s (no values)', item), 'Interpreter', 'none', ...
              'FontWeight', 'normal');
        continue
    end
    looped = vals(mod(0:N-1, numel(vals)) + 1);
    stem(ax, 1:N, looped, 'filled', 'Color', cPacer, ...
         'MarkerFaceColor', cPacer, 'MarkerSize', 3);
    title(ax, item, 'Interpreter', 'none', 'FontWeight', 'normal');
end

% share the element-index axis across the lower (index-space) panels only
linkaxes(idxAxes, 'x');
xlim(idxAxes(1), [0.5 N + 0.5]);
xlabel(idxAxes(end), 'pacer element index');

if nargout == 0, clear fig; end
end


% ==== local helpers ======================================================

function s = driftOverlay(ax, d, iv)
% Underlay for a drifted stream's interval panel: the steady lead-in (grey),
% each hold-and-ramp ramp (amber), the bound (dotted) and the base interval
% (dashed). Returns a one-line summary of the drift for the panel title.
    cLead = [0.60 0.60 0.60];  cRamp = [0.90 0.70 0.25];  cBnd = [0.45 0.45 0.45];
    n      = numel(iv);
    base   = double(getField(d, 'BaseInterval', mean(iv)));
    steady = double(getField(d, 'SteadyIntervals', 0));
    params = getField(d, 'Params', struct());
    r      = double(getField(params, 'RangeFrac', NaN));

    yl = [min(iv) max(iv)];
    if isfinite(r), yl = [min(yl(1), base/(1+r)) max(yl(2), base*(1+r))]; end
    pad = 0.05 * max(diff(yl), eps);  ylo = yl(1) - pad;  yhi = yl(2) + pad;

    if steady > 0
        drawSpan(ax, 0.5, steady + 0.5, ylo, yhi, cLead, 0.18);
    end
    ramps = asStructList(getField(d, 'Ramps', {}));
    for i = 1:numel(ramps)
        drawSpan(ax, double(ramps{i}.Start) - 0.5, double(ramps{i}.End) + 0.5, ...
                 ylo, yhi, cRamp, 0.22);
    end
    if isfinite(r)
        yline(ax, base/(1+r), ':', 'Color', cBnd);
        yline(ax, base*(1+r), ':', 'Color', cBnd);
    end
    yline(ax, base, '--', 'Color', cBnd);
    ylim(ax, [ylo yhi]);

    m = char(string(getField(d, 'Method', '?')));
    switch m
        case 'RandomWalk'
            detail = sprintf('step %g, rho %g, %d bound hit(s)', ...
                             getField(params, 'StepFrac', NaN), getField(params, 'Rho', NaN), ...
                             double(getField(d, 'BoundHits', 0)));
        case 'Multisine'
            detail = sprintf('rms %g, %d freq(s)', getField(params, 'RmsFrac', NaN), ...
                             numel(getField(params, 'Freqs', [])));
        case 'HoldRamp'
            nTrunc = sum(cellfun(@(x) logical(getField(x, 'Truncated', false)), ramps));
            detail = sprintf('%d ramp(s)%s', numel(ramps), ...
                             repmat(', last truncated', 1, double(nTrunc > 0)));
        otherwise
            detail = '';
    end
    s = sprintf('%s (base %g ms, steady %d, %s, %d intervals)', m, base, steady, detail, n);
end

function D = asDrift(x)
% jsondecode gives a struct for a drift object, [] for null/absent.
    D = [];
    if iscell(x) && ~isempty(x), x = x{1}; end
    if isstruct(x) && ~isempty(x), D = x(1); end
end

function L = asStructList(x)
    L = {};
    if isempty(x), return; end
    if iscell(x)
        L = x(:)';
    elseif isstruct(x)
        L = arrayfun(@(s) s, x(:)', 'UniformOutput', false);
    end
end

function plotTicks(ax, x, yc, h, c)
% Vertical ticks at each x centred on yc, half-height h, as ONE line object.
    n = numel(x);
    xs = reshape([x(:)'; x(:)'; nan(1, n)], 1, []);
    ys = reshape([repmat(yc - h, 1, n); repmat(yc + h, 1, n); nan(1, n)], 1, []);
    plot(ax, xs, ys, '-', 'Color', c, 'LineWidth', 1.5);
end

function drawSpan(ax, x0, x1, ylo, yhi, c, a)
% Shaded band [x0 x1] over [ylo yhi], no edge. Handles reversed x gracefully.
    xl = min(x0, x1); xr = max(x0, x1);
    patch(ax, [xl xr xr xl], [ylo ylo yhi yhi], c, ...
          'FaceAlpha', a, 'EdgeColor', 'none');
end

function labelSpan(ax, x0, x1, ybase, str, c)
% Centred label just above the band's bottom edge.
    xc = (x0 + x1) / 2;
    text(ax, xc, ybase + 0.08, str, 'Color', c, 'FontSize', 8, ...
         'HorizontalAlignment', 'center', 'VerticalAlignment', 'bottom', ...
         'Interpreter', 'none');
end

function v = asRow(x)
    if iscell(x), x = cell2mat(x); end
    v = double(x(:))';
end

function P = asProfileList(pp)
    P = {};
    if isempty(pp), return; end
    if iscell(pp)
        P = pp(:)';
    elseif isstruct(pp)
        P = arrayfun(@(s) s, pp(:)', 'UniformOutput', false);
    end
end

function v = getField(s, f, d)
    if isstruct(s) && isfield(s, f) && ~isempty(s.(f))
        v = s.(f);
    else
        v = d;
    end
end
