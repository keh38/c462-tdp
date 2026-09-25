function [T, info] = loadIntervalTable(name)
%LOADINTERVALTABLE  Load a curated interval table (rows of intervals) for drawing.
%   T = tapping.loadIntervalTable(name) reads the Excel table <name>.xlsx from the
%   current working directory (the generator's sandbox) and returns it as an R x C
%   numeric matrix: R rows, each a set of C intervals in ms. Draw whole rows from it
%   with tapping.drawTableRows.
%
%   [T, info] = tapping.loadIntervalTable(name) also returns identity metadata for
%   the provenance stamp:
%       info.Name  info.Path  info.Rows  info.Cols  info.Hash   (MD5 of the file)
%   Record info in the trial-list provenance so a run is reproducible against the
%   EXACT table it drew from -- the same seed on a changed table is a different draw,
%   and the hash is what detects that the table version moved.
%
%   The sheet must be a plain numeric matrix: every cell a finite interval > 0, with
%   NO header row, NO label column, and NO blank cells (a blank reads as NaN and
%   errors). The table is a config resource the tool places in the sandbox before the
%   run; this function only reads and validates it -- it never samples (that is
%   drawTableRows, so the randomness stays seeded and reproducible).

    arguments
        name {mustBeTextScalar}
    end
    name = char(name);

    % Resolve <name>.xlsx in the current directory (the sandbox the generator runs in).
    [~, ~, ext] = fileparts(name);
    if isempty(ext), name = [name '.xlsx']; end
    if ~isfile(name)
        error('tapping:loadIntervalTable:notFound', ...
            'Interval table not found in the working directory: %s', name);
    end

    T = readmatrix(name);      % MATLAB's Excel reader; sheet 1 by default

    % ---- validate: a plain, finite, positive numeric matrix --------------------
    if isempty(T) || ~isnumeric(T)
        error('tapping:loadIntervalTable:empty', ...
            'Table %s did not read as a non-empty numeric matrix.', name);
    end
    if any(~isfinite(T(:)))
        error('tapping:loadIntervalTable:nan', ...
            ['Table %s has non-numeric or missing cells (NaN/Inf). The sheet must be ', ...
             'a plain numeric matrix -- no header row, no label column, no blanks.'], name);
    end
    if any(T(:) <= 0)
        error('tapping:loadIntervalTable:nonpositive', ...
            'Table %s has values <= 0; every interval must be finite and > 0 (ms).', name);
    end

    if nargout > 1
        info = struct( ...
            'Name', name, ...
            'Path', fullfile(pwd, name), ...
            'Rows', size(T, 1), ...
            'Cols', size(T, 2), ...
            'Hash', fileMD5(fullfile(pwd, name)));
    end
end

% ---- local: MD5 of the file bytes, toolbox-free (best-effort) ------------------
% Optional. If you don't need table-version integrity, delete this and info.Hash;
% recording info.Name alone still identifies the source table.
function h = fileMD5(path)
    h = '';
    try
        fid = fopen(path, 'r');
        bytes = fread(fid, Inf, '*uint8');
        fclose(fid);
        md  = java.security.MessageDigest.getInstance('MD5');
        raw = md.digest(typecast(bytes, 'int8'));       % lossless uint8 -> int8 for Java
        h   = sprintf('%02x', typecast(int8(raw), 'uint8'));
    catch
        % leave empty -- hash is best-effort and must never break a run
    end
end
