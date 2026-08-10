function previewTrialList(json)

% writelines(json, 'tmp.json');

trialList = jsondecode(json);

tapping.previewTrial(trialList.Trials(1));
