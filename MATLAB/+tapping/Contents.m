% TAPPING  Shared primitives for building tapping trial lists.
%
%   Vector primitives (produce/transform interval vectors):
%     drawFromSet        - draw n values from a set (replace, weights optional)
%     drawToDuration     - draw values until cumulative duration exceeds a target
%     drawSumConstrained - draw n values summing exactly to a target
%     tilePattern        - tile a pre-drawn unit to a count; returns [intervals, unit]
%     loadIntervalTable  - load a curated interval table (+ identity info)
%     drawTableRows      - draw whole rows from an interval table
%
%   Drifting streams (non-repeating; return [intervals, drift record]):
%     driftRandomWalk    - interval wanders as a bounded (reflected) random walk
%     driftMultisine     - interval modulated by a sum of sines, random phases
%     driftHoldRamp      - constant-interval plateaus joined by steady ramps
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
%     previewTrial       - per-trial view: onset times, jitter, drift, profiles
%     patternLength      - a stream's period (empty pattern = full length)
%
%   Add the folder CONTAINING +tapping to the path (not +tapping itself),
%   then call as tapping.drawFromSet(...), tapping.previewTrial(...), etc.
