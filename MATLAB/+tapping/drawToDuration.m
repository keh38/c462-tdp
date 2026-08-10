function v = drawToDuration(set, targetMs, replace, weights)
%TAPPING.DRAWTODURATION  Draw intervals until their cumulative duration exceeds
%   targetMs. The count is an OUTPUT, not an input - a different objective from
%   drawFromSet, which is why this is its own function rather than a flag.
%   Composes drawFromSet one draw at a time.
%
%   v = tapping.drawToDuration(set, targetMs)
%   v = tapping.drawToDuration(set, targetMs, replace, weights)
    if nargin < 3, replace = true; end
    if nargin < 4, weights = []; end
    v = []; total = 0;
    while total < targetMs
        x = tapping.drawFromSet(set, 1, replace, weights);   % qualified sibling call
        v(end+1) = x;                                         %#ok<AGROW>
        total = total + x;
    end
end
