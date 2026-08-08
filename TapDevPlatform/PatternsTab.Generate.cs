using System;
using System.Threading.Tasks;
using System.Windows.Forms;

using TDP.Api;   // ConversationManager, MarkerDispatcher, DispatchedReply, ReplyKind

namespace TapDevPlatform
{
    /// <summary>
    /// Substage-4c generate sequence, as a partial of MainForm.
    ///
    /// IMPORTANT: this defines OnGenerateClicked. DELETE the 4b stub version of
    /// OnGenerateClicked in PatternsTab.MainForm.cs so this is the only definition —
    /// two partials can't both declare the same method. Your 4b sandbox-write code
    /// moves into WriteGeneratorToSandbox below.
    ///
    /// The shape: run -> validate -> on failure feed the error back into the SAME
    /// conversation, get a corrected generator, try again — up to a cap. The three
    /// failure modes are handled distinctly:
    ///   1) MATLAB engine not initialized  -> message, STOP (not repairable by the model).
    ///   2) MATLAB throws running the .m    -> feed the exception back (repair loop).
    ///   3) validateTrialList rejects output-> feed the report back (repair loop).
    /// </summary>
    public partial class MainForm : Form
    {
        // Initial run + up to (cap - 1) automatic corrections, then stop and surface.
        private const int MaxGenerateAttempts = 3;

        private async void OnGenerateClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_pendingGeneratorCode)) return;

            // --- Failure mode 1: engine not initialized ---
            // Not repairable by the model — it's an environment problem, not a bad
            // generator. Say so and STOP. We return BEFORE SetBusy and before the
            // finally that clears _pendingGeneratorCode, so the generator stays armed:
            // fix MATLAB, click Generate again, no need to re-author.
            if (!MATLAB.IsInitialized)
            {
                AppendTranscript("Note",
                    "MATLAB engine isn\u2019t initialized \u2014 can\u2019t run the generator. " +
                    "Start the engine and click Generate again.",
                    NoteColor, boldLabel: true);
                return;
            }

            SetBusy(true);
            try
            {
                await RunWithRepairAsync(_pendingGeneratorCode);
            }
            catch (Exception ex)
            {
                // A failure outside the handled MATLAB/validation paths — e.g. the
                // API call inside a repair turn threw. History stayed clean on that
                // throw (ConversationManager guarantees it), so nothing is corrupted.
                AppendTranscript("Error", ex.Message, ErrorColor, boldLabel: true);
            }
            finally
            {
                // The generator has been consumed (run, corrected past, or given up on).
                // Disarm; the next chat turn will arm a fresh one if it returns GENERATOR.
                _pendingGeneratorCode = null;
                SetBusy(false);
            }
        }

        /// <summary>
        /// One run attempt, then repair-and-retry up to <see cref="MaxGenerateAttempts"/>.
        /// Every attempt and every failure is written to the transcript so the loop is
        /// visible, not silent.
        /// </summary>
        private async Task RunWithRepairAsync(string code)
        {
            for (int attempt = 1; attempt <= MaxGenerateAttempts; attempt++)
            {
                // Write the .m to the sandbox (your seam).
                string mPath = WriteGeneratorToSandbox(code);
                if (mPath == null)
                {
                    AppendTranscript("Note",
                        "Sandbox write isn\u2019t wired yet \u2014 fill in WriteGeneratorToSandbox().",
                        NoteColor, boldLabel: true);
                    return;
                }

                // --- Failure mode 2: MATLAB throws running the generator ---
                string jsonPath;
                try
                {
                    jsonPath = await RunGeneratorAsync(mPath);
                }
                catch (Exception ex)
                {
                    AppendTranscript($"Run failed (attempt {attempt})", ex.Message, ErrorColor, boldLabel: true);
                    if (attempt == MaxGenerateAttempts) { AnnounceGaveUp(); return; }

                    code = await RepairAsync(
                        "The generator threw an error when run in MATLAB. Fix it.\n\n" + ex.Message);
                    if (code == null) return;   // model needs the human — stop the auto-loop
                    continue;
                }

                // --- Failure mode 3: validation rejects the output ---
                ValidationResult v = await ValidateAsync(jsonPath);
                if (!v.Ok)
                {
                    AppendTranscript($"Validation failed (attempt {attempt})", v.Report, ErrorColor, boldLabel: true);
                    if (attempt == MaxGenerateAttempts) { AnnounceGaveUp(); return; }

                    code = await RepairAsync(
                        "validateTrialList rejected the output. Fix the generator.\n\n" + v.Report);
                    if (code == null) return;
                    continue;
                }

                // --- success ---
                AppendTranscript("Note", $"Validated OK (attempt {attempt}).", NoteColor, boldLabel: false);
                await DeliverValidatedListAsync(jsonPath, code);
                return;
            }
        }

        /// <summary>
        /// Feeds a failure back into the conversation as the next user turn and returns
        /// the corrected generator — or null if the model replied with something that
        /// ISN'T a generator (a QUESTION it needs answered, a CANNOT, or an unmarked
        /// reply). In that case the automatic loop stops and the user takes over in chat.
        ///
        /// This costs nothing extra: history already holds the failed generator as an
        /// assistant turn, and ConversationManager re-sends the whole conversation, so
        /// the model sees exactly what it produced plus the error. No retry bookkeeping,
        /// no special state — the repair is just another turn.
        /// </summary>
        private async Task<string> RepairAsync(string errorText)
        {
            string reply = await _conversation.SendAsync(errorText);
            DispatchedReply d = MarkerDispatcher.Parse(reply);
            HandleReply(d);   // render the terse marker / question / cannot in the transcript
            return d.Kind == ReplyKind.Generator ? d.Body : null;
        }

        private void AnnounceGaveUp()
        {
            AppendTranscript("Note",
                $"Gave up after {MaxGenerateAttempts} attempts. Tell it what to change, or revise by hand.",
                NoteColor, boldLabel: true);
        }

        // =====================================================================
        //  STUBS — wire these to your existing .NET <-> MATLAB interface and the
        //  stage-2 playback path. The SIGNATURES are the contract the loop above
        //  relies on; fill the bodies. Add `async` to the Task-returning ones when
        //  you implement them (left non-async here only so the stubs compile clean).
        // =====================================================================

        /// <summary>
        /// Writes the generator to the sandbox and returns the .m path (or null if not
        /// wired). This is your 4b sandbox seam: set the folder and the filename policy
        /// (overwrite one fixed name, or version per iteration) here.
        /// </summary>
        private string WriteGeneratorToSandbox(string code)
        {
            string filename = $"gen_{_subjectName}_{DateTime.Now:yyyyMMdd_HHmmss}.m";
            string mPath = Path.Combine(FileLocations.GeneratorFolder, filename);
            File.WriteAllText(mPath, code);

            AppendTranscript("Note", $"Generator written to:\n{mPath}", NoteColor, boldLabel: false);
            return mPath;
        }

        /// <summary>
        /// Runs the generator .m via the MATLAB engine (working dir = sandbox) and
        /// returns the path to the Tapping.&lt;name&gt;.json it wrote. MUST THROW on a
        /// MATLAB error — that exception's message is what RepairAsync feeds back.
        /// Keep the call awaited so the UI stays live; use your existing Elements/
        /// analysis MATLAB path rather than inventing a new one.
        /// </summary>
        private async Task<string> RunGeneratorAsync(string mPath)
        {
            await Task.Run(() => MATLAB.RunGenerator(mPath));
            return FileLocations.CurrentTry;
        }

        /// <summary>
        /// Runs tapping.validateTrialList on the output and returns Ok + the report.
        /// A structural failure is NOT an exception here — it is Ok=false with the
        /// report text, which is what gets fed back to the model on failure mode 3.
        /// (Reserve exceptions for "couldn't run the validator at all".)
        /// </summary>
        private async Task<ValidationResult> ValidateAsync(string jsonPath)
        {
            (bool ok, string report) = await Task.Run(() => MATLAB.ValidateTrialList(jsonPath));
            return new ValidationResult(ok, report);
        }

        private sealed class ValidationResult
        {
            public bool Ok { get; }
            public string Report { get; }
            public ValidationResult(bool ok, string report)
            {
                Ok = ok;
                Report = report ?? string.Empty;
            }
        }
    }
}
