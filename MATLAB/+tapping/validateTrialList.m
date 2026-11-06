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
%   Drift records (PacerDrift / DistractorDrift, optional): when present, the
%   record must describe its stream - known Method, empty Pattern field, the
%   first SteadyIntervals intervals equal to BaseInterval, every interval inside
%   the record's RangeFrac bound, and (HoldRamp) ordered ramp indices within the
%   stream. Parameter values outside the suggested ranges are NOT errors.
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

        % --- drift records: describe their stream exactly (see checkDrift)
        if hasDrift(t, 'PacerDrift')
            errors = [errors, checkDrift(t.PacerDrift, pac, getNum(t, 'PacerPattern'), ...
                                         who, 'PacerDrift', 'PacerPattern')]; %#ok<AGROW>
        end
        if hasDrift(t, 'DistractorDrift')
            if isempty(dis)
                errors{end+1} = sprintf('%s: DistractorDrift is present but there is no distractor', who); %#ok<AGROW>
            else
                errors = [errors, checkDrift(t.DistractorDrift, dis, getNum(t, 'DistractorPattern'), ...
                                             who, 'DistractorDrift', 'DistractorPattern')]; %#ok<AGROW>
            end
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

function tf = hasDrift(t, f)
% A drift field is optional: absent, null ([] after jsondecode) or empty = none.
    tf = isstruct(t) && isfield(t, f) && ~isempty(t.(f));
end

function errs = checkDrift(d, iv, pat, who, name, patName)
% A drift record must be consistent with the interval vector it describes. These
% are malformity checks (a record that contradicts its stream), not judgments of
% the design: parameter values outside the suggested ranges are legal.
    errs = {};
    if iscell(d), d = d{1}; end
    if ~isstruct(d)
        errs{end+1} = sprintf('%s: %s is not an object', who, name);
        return
    end
    methods = {'RandomWalk', 'Multisine', 'HoldRamp'};
    m = char(string(getField(d, 'Method', '')));
    if ~any(strcmp(m, methods))
        errs{end+1} = sprintf('%s: %s.Method = "%s" is not in {RandomWalk, Multisine, HoldRamp}', who, name, m);
    end

    % a drifted stream never repeats: drift primitives do not tile
    if ~isempty(pat)
        errs{end+1} = sprintf('%s: %s is present, so %s must be empty (a drifted stream does not repeat)', ...
                              who, name, patName);
    end

    base = getScalar(d, 'BaseInterval', NaN);
    if ~isfinite(base) || base <= 0
        errs{end+1} = sprintf('%s: %s.BaseInterval must be finite and > 0', who, name);
        return
    end

    % the steady lead-in is exactly the base interval, and leaves room to drift
    ns = getScalar(d, 'SteadyIntervals', NaN);
    if ~isfinite(ns) || ns < 0 || ns ~= floor(ns)
        errs{end+1} = sprintf('%s: %s.SteadyIntervals must be a non-negative integer', who, name);
    elseif ns >= numel(iv)
        errs{end+1} = sprintf('%s: %s.SteadyIntervals (%d) leaves no drifting intervals (stream has %d)', ...
                              who, name, ns, numel(iv));
    else
        off = find(abs(iv(1:ns) - base) > 1e-6 * base);
        if ~isempty(off)
            errs{end+1} = sprintf('%s: %s.SteadyIntervals = %d, but interval(s) %s differ from BaseInterval %g ms', ...
                                  who, name, ns, mat2str(off), base);
        end
    end

    % every interval within the record's bound
    params = getField(d, 'Params', struct());
    r = getScalar(params, 'RangeFrac', NaN);
    if isfinite(r) && r > 0
        lo = base / (1 + r);  hi = base * (1 + r);  tol = 1e-9 * base;
        out = find(iv < lo - tol | iv > hi + tol);
        if ~isempty(out)
            errs{end+1} = sprintf('%s: %s bound [%.1f, %.1f] ms (RangeFrac %g) exceeded at interval(s) %s', ...
                                  who, name, lo, hi, r, mat2str(out));
        end
    end

    % hold-and-ramp: ramp events index into the stream, in order, after the lead-in
    if strcmp(m, 'HoldRamp')
        R = asCellOfStructs(getField(d, 'Ramps', {}));
        lastEnd = 0;
        if isfinite(ns), lastEnd = ns; end
        for i = 1:numel(R)
            s = getScalar(R{i}, 'Start', NaN);  e = getScalar(R{i}, 'End', NaN);
            dirn = char(string(getField(R{i}, 'Direction', '')));
            rwho = sprintf('%s: %s ramp %d', who, name, i);
            if ~all(isfinite([s e])) || s ~= floor(s) || e ~= floor(e) || s > e || s <= lastEnd || e > numel(iv)
                errs{end+1} = sprintf('%s: Start/End (%g, %g) are not ordered indices within %d..%d', ...
                                      rwho, s, e, lastEnd + 1, numel(iv)); %#ok<AGROW>
            else
                lastEnd = e;
            end
            if ~any(strcmp(dirn, {'Faster', 'Slower'}))
                errs{end+1} = sprintf('%s: Direction = "%s" is not in {Faster, Slower}', rwho, dirn); %#ok<AGROW>
            end
        end
    end
end

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
