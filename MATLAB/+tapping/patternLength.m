function n = patternLength(pattern, intervals)
%PATTERNLENGTH Period of a stream under the empty-means-non-repeating convention.
%   n = tapping.patternLength(pattern, intervals) returns the length of the
%   repeating unit for a stream:
%       - if PATTERN is non-empty, the period is numel(PATTERN);
%       - if PATTERN is empty, the stream does not repeat and its period is the
%         full flattened length, numel(INTERVALS).
%
%   This is the ONE place the empty->full rule lives. Analysis code must call
%   this rather than numel(pattern) directly, which would return 0 for a
%   non-repeating stream and misread it as length zero.
%
%   Distractor caveat: an ABSENT distractor (empty INTERVALS) has no period.
%   Callers should check for a present distractor first; here, empty INTERVALS
%   with empty PATTERN yields 0, meaning "not applicable", not "non-repeating".

    if ~isempty(pattern)
        n = numel(pattern);
    else
        n = numel(intervals);
    end
end
