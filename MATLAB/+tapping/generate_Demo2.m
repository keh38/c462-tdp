%% Tapping generator - example 2, using the +tapping library
% Qualified calls (tapping.<fn>) are library primitives; bare calls are this
% generator's own wiring. No num2cell here - writeTrialList centralizes it.
%
%   Pacer      set {500,1000,1500} ms weighted 1:2:1; draw 4, tile 20x
%   Distractor set 500:100:1000 ms drawn just past the pacer duration (no loop)
%   LeadIn     two pacer patterns
%   Frequency  Sound alternates 1000 / 2000 Hz across elements
%   Pacer role A on every trial;  3 trials, each a fresh draw

%% Parameters
seed          = 42;
pacerSet      = [500 1000 1500];
pacerWeights  = [1 2 1];
patternLen    = 4;
nRepeats      = 20;
distractorSet = 500:100:1000;
freqItem      = "Sound.Tone.Frequency_Hz";
freqValues    = [1000 2000];
nTrials       = 3;
listName      = "Demo2";                 % -> Tapping.Demo2.json

rng(seed);                                % before any draw - the record

%% Build (Pacer = A on all, so no A/B assembly step)
trials = {};
for k = 1:nTrials
    pattern      = tapping.drawFromSet(pacerSet, patternLen, true, pacerWeights);
    pacerIv      = repmat(pattern, 1, nRepeats);
    leadIn       = 2 * sum(pattern);
    distractorIv = tapping.drawToDuration(distractorSet, sum(pacerIv), true, []);

    t = tapping.newTrial();
    t.Tag                 = sprintf('rule3_t%02d', k);
    t.Pacer               = "A";
    t.LeadIn              = leadIn;
    t.PacerIntervals      = pacerIv;            % plain numeric - writeTrialList wraps
    t.DistractorIntervals = distractorIv;
    t.ParameterProfiles   = { tapping.makeProfile(freqItem, freqValues) };

    % distractor-equals-pacer case (for reference) would simply be:
    %   t.DistractorIntervals = pacerIv;   t.Offset = someMs;

    trials{end+1} = t;                          %#ok<AGROW>
end

%% Write (owns Tapping.<name>.json + provenance + array wrapping)
outFile = tapping.writeTrialList(trials, listName, seed);
fprintf('wrote %s (%d trials)\n', outFile, numel(trials));
