function report = validateTrialList(jsonPath)
%TAPPING.VALIDATETRIALLIST  Structural + sanity gate for a trial-list file.
%   report = tapping.validateTrialList('Tapping.Demo2.json')
%
%   Checks that the file is WELL-FORMED and SANE - not that it implements the
%   experiment you intended (that is the preview's job and yours). Collects ALL
%   issues, prints a readable per-trial report, and returns:
%     report.ok        true when there are no errors
%     report.errors    cellstr of "trial k: ..." messages
%     report.warnings  cellstr (reserved; this gate is errors-only by design)
%     report.nTrials
%
%   This gate is errors-only on purpose. "Valid but perhaps unintended" cases
%   (e.g. a distractor loop that rotates against the pacer) are NOT malformity -
%   there is nothing to fail on - so they belong to the preview, not here.
%
%   Single-element arrays: MATLAB's jsondecode cannot tell a JSON scalar from a
%   1-element JSON array, so this validator does not police array-wrapping. A
%   genuinely collapsed array fails LOUD in the C# loader (Newtonsoft will not
%   read a scalar into float[]), so that case is caught downstream instead.

    errors = {}; warnings = {};

    if ~(ischar(jsonPath) || isstring(jsonPath)) || ~isfile(jsonPath)
        error('tapping:validateTrialList:noFile', 'file not found: %s', string(jsonPath));
    end

    raw = fileread(jsonPath);
    try
        data = jsondecode(raw);
    catch me
        report = finish({sprintf('JSON does not parse: %s', me.message)}, {}, 0);
        return
    end

    if ~isfield(data, 'Trials') || isempty(data.Trials)
        report = finish({'no Trials in file (or Trials is empty)'}, {}, 0);
        return
    end

    trials = asCellOfStructs(data.Trials);
    n = numel(trials);
    validPacer = {'A', 'B'};
    validResp  = {'AllElements', 'DownbeatOnly'};

    for k = 1:n
        t = trials{k};
        who = sprintf('trial %d', k);
        tag = char(string(getField(t, 'Tag', '')));
        if ~isempty(tag), who = sprintf('trial %d (%s)', k, tag); end

        % --- pacer: present, non-empty, finite, positive (it defines the trial)
        pac = getNum(t, 'PacerIntervals');
        if isempty(pac)
            errors{end+1} = sprintf('%s: PacerIntervals is empty - the pacer defines the trial', who); %#ok<AGROW>
        else
            errors = [errors, checkIntervals(pac, who, 'PacerIntervals')]; %#ok<AGROW>
        end

        % --- distractor: may be empty (pacer-only); if present, finite + positive
        dis = getNum(t, 'DistractorIntervals');
        if ~isempty(dis)
            errors = [errors, checkIntervals(dis, who, 'DistractorIntervals')]; %#ok<AGROW>
        end

        % --- LeadIn / Offset: finite; LeadIn >= 0; distractor not before t=0
        li = getScalar(t, 'LeadIn', 0);
        of = getScalar(t, 'Offset', 0);
        if ~isfinite(li)
            errors{end+1} = sprintf('%s: LeadIn is not finite', who); %#ok<AGROW>
        elseif li < 0
            errors{end+1} = sprintf('%s: LeadIn is negative (%g ms)', who, li); %#ok<AGROW>
        end
        if ~isfinite(of)
            errors{end+1} = sprintf('%s: Offset is not finite', who); %#ok<AGROW>
        end
        if isfinite(li) && isfinite(of) && (li + of) < 0
            errors{end+1} = sprintf('%s: LeadIn+Offset < 0 - distractor would start before t=0', who); %#ok<AGROW>
        end

        % --- enums must be in their legal sets
        pv = char(string(getField(t, 'Pacer', '')));
        if ~any(strcmp(pv, validPacer))
            errors{end+1} = sprintf('%s: Pacer = "%s" is not in {A, B}', who, pv); %#ok<AGROW>
        end
        rv = char(string(getField(t, 'ResponseInstructions', 'AllElements')));
        if ~any(strcmp(rv, validResp))
            errors{end+1} = sprintf('%s: ResponseInstructions = "%s" is not in {AllElements, DownbeatOnly}', who, rv); %#ok<AGROW>
        end

        % --- parameter profiles: Item non-empty, Values non-empty + finite
        profs = getProfiles(t);
        for i = 1:numel(profs)
            item  = char(string(getField(profs{i}, 'Item', '')));
            vals  = getNum(profs{i}, 'Values');
            pwho  = sprintf('%s profile %d', who, i);
            if isempty(item)
                errors{end+1} = sprintf('%s: Item is empty', pwho); %#ok<AGROW>
            end
            if isempty(vals)
                errors{end+1} = sprintf('%s (%s): Values is empty', pwho, item); %#ok<AGROW>
            elseif any(~isfinite(vals))
                errors{end+1} = sprintf('%s (%s): Values contain NaN/Inf', pwho, item); %#ok<AGROW>
            end
            % positivity is NOT checked: Item is opaque and some parameters
            % (detunings, phases) may legitimately be signed.
        end
    end

    report = finish(errors, warnings, n);
end


% ==== local plumbing (private to this gate) ==============================

function errs = checkIntervals(v, who, name)
    errs = {};
    bad = find(~isfinite(v));
    if ~isempty(bad)
        errs{end+1} = sprintf('%s: %s has NaN/Inf at position(s) %s', who, name, mat2str(bad));
    end
    nonpos = find(isfinite(v) & v <= 0);
    if ~isempty(nonpos)
        errs{end+1} = sprintf('%s: %s has non-positive value(s) at position(s) %s', who, name, mat2str(nonpos));
    end
end

function report = finish(errors, warnings, n)
    report.ok       = isempty(errors);
    report.errors   = strjoin(errors, '\n');
    report.warnings = warnings;
    report.nTrials  = n;
    if report.ok
        fprintf('validate: OK - %d trial(s), no errors\n', n);
    else
        fprintf('validate: FAILED - %d error(s) across %d trial(s)\n', numel(errors), n);
    end
    for i = 1:numel(errors),   fprintf('  ERROR    %s\n', errors{i});   end
    for i = 1:numel(warnings), fprintf('  WARNING  %s\n', warnings{i}); end
end

function c = asCellOfStructs(x)
    if iscell(x)
        c = x(:)';
    elseif isstruct(x)
        c = arrayfun(@(s) s, x(:)', 'UniformOutput', false);
    else
        c = {};
    end
end

function v = getNum(s, f)
    if isstruct(s) && isfield(s, f) && ~isempty(s.(f))
        x = s.(f);
        if iscell(x), x = cell2mat(x); end
        v = double(x(:))';
    else
        v = [];
    end
end

function v = getScalar(s, f, d)
    if isstruct(s) && isfield(s, f) && ~isempty(s.(f)) && isnumeric(s.(f))
        x = s.(f);
        v = double(x(1));
    else
        v = d;
    end
end

function v = getField(s, f, d)
    if isstruct(s) && isfield(s, f) && ~isempty(s.(f))
        v = s.(f);
    else
        v = d;
    end
end

function P = getProfiles(t)
    P = {};
    if isstruct(t) && isfield(t, 'ParameterProfiles') && ~isempty(t.ParameterProfiles)
        pp = t.ParameterProfiles;
        if iscell(pp)
            P = pp(:)';
        elseif isstruct(pp)
            P = arrayfun(@(s) s, pp(:)', 'UniformOutput', false);
        end
    end
end
