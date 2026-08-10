function p = makeProfile(item, values)
%TAPPING.MAKEPROFILE  One ParameterProfile: a stimulus parameter path plus its
%   per-element values, which the HTS loops/broadcasts over the elements.
%   Values are stored plain; writeTrialList wraps them so a single-value
%   profile encodes as a JSON array, not a scalar.
%
%   Example:
%     tapping.makeProfile("Sound.Tone.Frequency_Hz", [1000 2000])
    p.Item   = char(string(item));
    p.Values = double(values(:))';
end
