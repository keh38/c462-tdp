function [rows, cols] = validateIntervalTable(name)

[~, info] = tapping.loadIntervalTable(name);
rows = info.Rows;
cols = info.Cols;