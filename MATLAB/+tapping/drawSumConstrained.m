function iv = drawSumConstrained(set, n, targetSum, exclude)
%TAPPING.DRAWSUMCONSTRAINED  Draw n values from `set` (with replacement) summing
%   EXACTLY to targetSum, optionally != `exclude`. Draws the first n-1 freely
%   (via drawFromSet) and solves the last for the sum; rejects if the last
%   falls outside the set or reproduces `exclude`.
%
%   Its own function because the objective (a sum guarantee) and failure mode
%   (the search may not converge) differ from a free draw.
    if nargin < 4, exclude = []; end
    for attempt = 1:1000
        head = tapping.drawFromSet(set, n-1, true);          % qualified sibling call
        last = targetSum - sum(head);
        if ~ismember(last, set), continue; end
        iv = [head, last];
        if ~isempty(exclude) && isequal(iv, exclude), continue; end
        return
    end
    error('tapping:drawSumConstrained:noSolution', ...
          'no valid draw found in 1000 attempts (target sum %g ms)', targetSum);
end
