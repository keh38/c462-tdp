function t = newTrial()
%NEWTRIAL Blank trial struct with every field defaulted (mirrors the C# type).
%
%   The pattern fields default to EMPTY, and that default is load-bearing: it is
%   the sentinel for a non-repeating stream. A generator that draws-and-tiles
%   overwrites the relevant pattern field via tapping.tilePattern; a generator
%   that authors literal intervals with no repeat leaves it empty. So:
%       empty pattern  -> stream does not repeat; its period is numel(Intervals)
%       non-empty      -> the repeating unit the intervals were tiled from
%   Because only tilePattern ever sets a pattern non-empty, this holds with no
%   decision anywhere: not tiled -> stayed empty -> non-repeating.

    t = struct();

    t.Tag                  = '';
    t.Pacer                = 'A';            % 'A' | 'B'
    t.ResponseInstructions = 'AllElements';  % 'AllElements' | 'DownbeatOnly'

    t.LeadIn = 0;
    t.Offset = 0;

    t.PacerIntervals      = [];
    t.DistractorIntervals = [];              % empty = pacer-only trial

    % Authored repeating unit per stream, before tiling. Empty = non-repeating.
    % Set ONLY by tapping.tilePattern; never by copying the flattened intervals.
    t.PacerPattern      = [];
    t.DistractorPattern = [];

    t.ParameterProfiles = repmat(tapping.makeProfile('', []), 0, 1);  % empty profile list
end
