function rows = drawTableRows(T, n, replace)
%DRAWTABLEROWS  Draw whole rows at random from a curated interval table.
%   rows = tapping.drawTableRows(T, n)            draws n rows WITH replacement
%   rows = tapping.drawTableRows(T, n, replace)   replace=false => n DISTINCT rows
%
%   T is an R x C interval matrix from tapping.loadIntervalTable. Returns an n x C
%   matrix -- the drawn rows, in draw order. The draw uses MATLAB's rng, so seed it
%   in the generator (rng(seed)) BEFORE calling: the seed plus the table is the
%   reproducible record, and the JSON is one draw from it.
%
%   replace defaults to true (matching drawFromSet). "Concatenate N rows" does not
%   itself say whether repeats are allowed -- if the request is silent on that, ask
%   rather than assume, then pass replace explicitly.
%
%   Composition lives in the GENERATOR, not here. A drawn row is role-agnostic --
%   assign the result to PacerIntervals OR DistractorIntervals as the request wants:
%
%     * one drawn row, played once (non-repeating):
%           r = tapping.drawTableRows(T, 1);
%           t.PacerIntervals = r;            % 1 x C
%           t.PacerPattern   = [];           % not tiled -> non-repeating
%
%     * one drawn row, tiled to fill a longer stream (the row IS the repeating unit):
%           r = tapping.drawTableRows(T, 1);
%           [t.PacerIntervals, t.PacerPattern] = tapping.tilePattern(r, nPacer);
%
%     * N rows concatenated into one sequence, played once:
%           rows = tapping.drawTableRows(T, N, false);   % distinct rows
%           t.PacerIntervals = reshape(rows.', 1, []);   % row1, row2, ... in order
%           t.PacerPattern   = [];

    arguments
        T {mustBeNumeric, mustBeNonempty}
        n (1,1) {mustBeInteger, mustBePositive}
        replace (1,1) logical = true
    end

    R = size(T, 1);
    if ~replace && n > R
        error('tapping:drawTableRows:tooMany', ...
            'Asked for %d distinct rows but the table has only %d.', n, R);
    end

    if replace
        idx = randi(R, n, 1);
    else
        idx = randperm(R, n).';
    end
    rows = T(idx, :);
end
