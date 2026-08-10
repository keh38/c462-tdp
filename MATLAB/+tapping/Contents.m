% TAPPING  Shared primitives for building tapping trial lists.
%
%   Vector primitives (produce/transform interval vectors):
%     drawFromSet        - draw n values from a set (replace, weights optional)
%     drawToDuration     - draw values until cumulative duration exceeds a target
%     drawSumConstrained - draw n values summing exactly to a target
%
%   Trial construction:
%     newTrial           - blank trial struct, every field defaulted
%     makeProfile        - one ParameterProfile (Item + per-element Values)
%
%   Output + gate:
%     writeTrialList     - encode + write Tapping.<name>.json (owns the
%                          filename contract, provenance, and array wrapping)
%     validateTrialList  - structural/sanity gate over a written file
%
%   Inspection:
%     previewList        - run-scale table: one row per trial (order, balance)
%     previewTrial       - per-trial view: onset times, jitter, profiles
%
%   Add the folder CONTAINING +tapping to the path (not +tapping itself),
%   then call as tapping.drawFromSet(...), tapping.previewTrial(...), etc.
