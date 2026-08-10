function summary = previewList(src)
%TAPPING.PREVIEWLIST  Run-scale summary of a whole trial list - one row/trial.
%   tapping.previewList('Tapping.Demo2.json')     % from a file
%   tapping.previewList(s.Trials)                  % from decoded trials
%   summary = tapping.previewList(...)             % also returns the rows
%
%   This is the RUN-scale companion to previewTrial (which is one trial deep).
%   It answers "is my run built the way I meant" - is the order right, are the
%   A/B pacers balanced, do same-rule trials look alike and different rules
%   look different - by compressing each trial to a single readable row. It is
%   NOT a timing view; per-trial structure is previewTrial's job.
%
%   Accepts a file path, the decoded top-level struct, a Trials struct array,
%   or a cell of trial structs.

    trials = resolveTrials(src);
    n = numel(trials);
    if n == 0
        fprintf('previewList: no trials\n');
        summary = struct([]);
        return
    end

    % header
    fprintf('  #  %-20s %-3s %4s %6s %7s %-8s %6s %6s  %s\n', ...
            'Tag', 'Pac', 'nEl', 'IPI', 'dur(s)', 'Dist', 'Lead', 'Off', 'Profile');
    fprintf('  %s\n', repmat('-', 1, 82));

    rows = repmat(struct('index',0,'tag','','pacer','','nEl',0,'meanIPI',NaN, ...
                         'durS',0,'dist','','leadIn',0,'offset',0,'profile',''), n, 1);
    nA = 0; nB = 0; totalDur = 0;

    for k = 1:n
        t   = trials{k};
        pac = getNum(t, 'PacerIntervals');
        dis = getNum(t, 'DistractorIntervals');
        tag = char(string(getField(t, 'Tag', '')));
        pv  = char(string(getField(t, 'Pacer', '?')));
        li  = getScalar(t, 'LeadIn', 0);
        of  = getScalar(t, 'Offset', 0);

        nEl = numel(pac);
        if nEl > 0
            meanIPI = mean(pac);
            durS    = sum(pac) / 1000;
            varies  = (max(pac) - min(pac)) > 1e-6;
            ipiStr  = sprintf('%d%s', round(meanIPI), ternary(varies, '~', ''));
        else
            meanIPI = NaN; durS = 0; ipiStr = '-';
        end

        if isempty(dis)
            distStr = '-';
        elseif numel(dis) == nEl && isequal(dis, pac)
            distStr = '=pacer';
        else
            distStr = sprintf('%d', numel(dis));
        end

        prof = profSummary(getProfiles(t));
        if strcmpi(pv, 'A'), nA = nA + 1; elseif strcmpi(pv, 'B'), nB = nB + 1; end
        totalDur = totalDur + durS;

        fprintf('  %2d %-20s %-3s %4d %6s %7.1f %-8s %6g %6g  %s\n', ...
                k, trunc(tag, 20), pv, nEl, ipiStr, durS, distStr, li, of, prof);

        rows(k) = struct('index', k, 'tag', tag, 'pacer', pv, 'nEl', nEl, ...
                         'meanIPI', meanIPI, 'durS', durS, 'dist', distStr, ...
                         'leadIn', li, 'offset', of, 'profile', prof);
    end

    fprintf('  %s\n', repmat('-', 1, 82));
    fprintf('  %d trials, total %.1f s (%.1f min); Pacer A:%d B:%d\n', ...
            n, totalDur, totalDur/60, nA, nB);

    if nargout > 0, summary = rows; end
end


% ==== local plumbing =====================================================

function s = profSummary(profs)
    if isempty(profs), s = '-'; return; end
    parts = {};
    for i = 1:numel(profs)
        item = char(string(getField(profs{i}, 'Item', '?')));
        d = strfind(item, '.');
        seg = item; if ~isempty(d), seg = item(d(end)+1:end); end   % last path segment
        vals = getNum(profs{i}, 'Values');
        parts{end+1} = sprintf('%s[%d]', seg, numel(vals)); %#ok<AGROW>
    end
    s = strjoin(parts, '+');
end

function trials = resolveTrials(x)
    if ischar(x) || isstring(x)
        data = jsondecode(fileread(char(x)));
        raw = data.Trials;
    elseif isstruct(x) && isfield(x, 'Trials')
        raw = x.Trials;
    else
        raw = x;
    end
    if iscell(raw)
        trials = raw(:)';
    elseif isstruct(raw)
        trials = arrayfun(@(s) s, raw(:)', 'UniformOutput', false);
    else
        trials = {};
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
        x = s.(f); v = double(x(1));
    else
        v = d;
    end
end

function v = getField(s, f, d)
    if isstruct(s) && isfield(s, f) && ~isempty(s.(f)), v = s.(f); else, v = d; end
end

function P = getProfiles(t)
    P = {};
    if isstruct(t) && isfield(t, 'ParameterProfiles') && ~isempty(t.ParameterProfiles)
        pp = t.ParameterProfiles;
        if iscell(pp), P = pp(:)';
        elseif isstruct(pp), P = arrayfun(@(s) s, pp(:)', 'UniformOutput', false);
        end
    end
end

function s = trunc(s, w)
    s = char(s);
    if numel(s) > w, s = [s(1:w-1) '.']; end
end

function out = ternary(c, a, b)
    if c, out = a; else, out = b; end
end
