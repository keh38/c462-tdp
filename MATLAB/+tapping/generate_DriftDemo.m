%% Tapping generator - drifting pacers, one trial per method (+tapping library)
%   Pacer      500 ms base interval, 8 steady intervals then drift, 45 s each
%                trial 1  bounded random walk   (defaults)
%                trial 2  sum of sines          (defaults)
%                trial 3  hold-and-ramp         (defaults)
%   Distractor none (pacer-only)
%   Pacer role A on every trial

%% Parameters
seed         = 7;
baseInterval = 500;          % ms
trialMs      = 45000;        % ms, steady lead-in included
listName     = "DriftDemo";  % -> Tapping.DriftDemo.json

rng(seed);                   % before any draw - the record

%% Build
trials = {};

t = tapping.newTrial();
t.Tag = 'drift_rw';
[t.PacerIntervals, t.PacerDrift] = tapping.driftRandomWalk(baseInterval, DurationMs=trialMs);
trials{end+1} = t;

t = tapping.newTrial();
t.Tag = 'drift_ms';
[t.PacerIntervals, t.PacerDrift] = tapping.driftMultisine(baseInterval, DurationMs=trialMs);
trials{end+1} = t;

t = tapping.newTrial();
t.Tag = 'drift_hr';
[t.PacerIntervals, t.PacerDrift] = tapping.driftHoldRamp(baseInterval, DurationMs=trialMs);
trials{end+1} = t;

%% Write
outFile = tapping.writeTrialList(trials, listName, seed);
fprintf('wrote %s (%d trials)\n', outFile, numel(trials));
