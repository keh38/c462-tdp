function v = drawFromSet(set, n, replace, weights)
%TAPPING.DRAWFROMSET  Draw n values from a set.
%   v = tapping.drawFromSet(set, n)                     uniform, with replacement
%   v = tapping.drawFromSet(set, n, replace)            replace = true|false
%   v = tapping.drawFromSet(set, n, replace, weights)   relative weights per element
%
%   `replace` and `weights` are PROPERTIES of one draw objective (the same
%   reason numpy's choice() carries replace= and p=), so they are parameters
%   here - not separate functions.
    if nargin < 3 || isempty(replace), replace = true; end
    if nargin < 4 || isempty(weights), weights = ones(1, numel(set)); end
    set = set(:)';  weights = weights(:)';

    if replace
        edges = [0, cumsum(weights) / sum(weights)];      % weighted bins on [0,1]
        v = set(discretize(rand(1, n), edges));
    else
        if n > numel(set)
            error('tapping:drawFromSet:tooMany', ...
                  'cannot draw %d distinct values from a %d-element set', n, numel(set));
        end
        keys = rand(1, numel(set)) .^ (1 ./ weights);     % Efraimidis-Spirakis
        [~, ord] = sort(keys, 'descend');                 % weighted, without replacement
        v = set(ord(1:n));
    end
end
