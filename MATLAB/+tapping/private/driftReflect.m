function [x, hit] = driftReflect(x, L)
%DRIFTREFLECT  Reflect a log-interval value back into [-L, L].
%   [x, hit] = driftReflect(x, L) mirrors X at the nearer bound until it lies
%   inside [-L, L] (repeated only if one step overshoots by more than 2L), and
%   reports whether any reflection happened. Reflection keeps the walk inside
%   the bound without piling values up at the edge, as clipping would.
    hit = false;
    while abs(x) > L
        x   = sign(x) * 2 * L - x;
        hit = true;
    end
end
