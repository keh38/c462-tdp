function outFile = writeTrialList(trials, name, seed, folder)
%TAPPING.WRITETRIALLIST  Encode a trial list to Tapping.<name>.json.
%   outFile = tapping.writeTrialList(trials, name, seed)
%   outFile = tapping.writeTrialList(trials, name, seed, folder)
%
%   TRIALS  cell array (or struct array) of trial structs from newTrial.
%   NAME    the <name> in the filename. The capital-T "Tapping." prefix is the
%           HTS config-file contract - NOT a typo, and NOT the +tapping package
%           name. Do not lowercase it.
%   SEED    the rng seed used by the generator; recorded for provenance so the
%           generator .m + this seed reproduce the file byte-for-byte.
%
%   This function owns three cross-cutting concerns so no generator has to:
%     1. the Tapping.<name>.json filename convention,
%     2. the provenance stamp,
%     3. the num2cell wrapping that stops length-1 arrays from collapsing to
%        JSON scalars (PacerIntervals, DistractorIntervals, each profile's
%        Values) and keeps the trial list a JSON array.
    if nargin < 3, seed = NaN; end
    if nargin < 4 || isempty(folder), folder = pwd; end

    if isstruct(trials), trials = num2cell(trials); end   % struct array -> cell (forces array)
    for k = 1:numel(trials)
        trials{k} = normalizeTrial(trials{k});
    end

    out.Trials     = trials;
    out.Provenance = struct('Seed', seed, 'Created', datestr(now, 31));

    json    = jsonencode(out, 'PrettyPrint', true);       % PrettyPrint: R2021a+
    outFile = fullfile(folder, sprintf('Tapping.%s.json', char(string(name))));
    fid = fopen(outFile, 'w');
    if fid < 0, error('tapping:writeTrialList:cannotOpen', 'cannot write %s', outFile); end
    fwrite(fid, json);  fclose(fid);
end


% ---- local plumbing (not part of the public package API) ----------------

function t = normalizeTrial(t)
% Wrap the array-valued leaf fields so single-element vectors survive as JSON
% arrays. Scalars (LeadIn, Offset) and enum strings are left untouched.
    t.PacerIntervals      = asCellArray(t.PacerIntervals);
    t.DistractorIntervals = asCellArray(t.DistractorIntervals);

    pp = t.ParameterProfiles;
    if ~isempty(pp)
        if isstruct(pp), pp = num2cell(pp); end
        for i = 1:numel(pp)
            pp{i}.Values = asCellArray(pp{i}.Values);
        end
        t.ParameterProfiles = pp;
    end
end

function c = asCellArray(x)
% Numeric vector -> row cell (so jsonencode emits a JSON array, even at len 1).
% Already-cell passes through; empty becomes {} -> JSON [].
    if iscell(x),   c = x;  return; end
    if isempty(x),  c = {}; return; end
    c = num2cell(double(x(:))');
end
