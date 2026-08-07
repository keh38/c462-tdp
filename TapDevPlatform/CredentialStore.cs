using System;
using System.Runtime.InteropServices;
using System.Text;

namespace TDP.Security   // rename to match your project
{
    /// <summary>
    /// Minimal wrapper over the Windows Credential Manager (Credential Vault).
    /// Stores a single secret string under a target name.
    ///
    /// Protection: the vault protects generic credentials with DPAPI scoped to the
    /// CURRENT USER. Only the Windows account that stored the secret can read it
    /// back; another user on the same machine cannot. The credential is visible and
    /// removable under Control Panel > Credential Manager > Windows Credentials.
    ///
    /// Persist = LOCAL_MACHINE here means "persists on THIS machine across logon
    /// sessions" — it does NOT mean other users on the machine can read it.
    ///
    /// Works identically on .NET Framework 4.x and modern .NET (6/7/8).
    /// </summary>
    public static class CredentialStore
    {
        private const int CRED_TYPE_GENERIC = 1;
        private const int CRED_PERSIST_LOCAL_MACHINE = 2;
        private const int ERROR_NOT_FOUND = 1168;
        private const int CRED_MAX_BLOB_BYTES = 2560;

        [StructLayout(LayoutKind.Sequential)]
        private struct CREDENTIAL
        {
            public int Flags;
            public int Type;
            public IntPtr TargetName;
            public IntPtr Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public int CredentialBlobSize;
            public IntPtr CredentialBlob;
            public int Persist;
            public int AttributeCount;
            public IntPtr Attributes;
            public IntPtr TargetAlias;
            public IntPtr UserName;
        }

        [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredWrite(ref CREDENTIAL credential, int flags);

        [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredRead(string target, int type, int reservedFlag, out IntPtr credentialPtr);

        [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredDelete(string target, int type, int reservedFlag);

        [DllImport("advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
        private static extern void CredFree(IntPtr cred);

        /// <summary>Stores (or overwrites) the secret under the given target name.</summary>
        public static void Save(string target, string secret)
        {
            if (string.IsNullOrEmpty(target)) throw new ArgumentNullException(nameof(target));
            if (secret == null) throw new ArgumentNullException(nameof(secret));

            byte[] blob = Encoding.Unicode.GetBytes(secret);
            if (blob.Length > CRED_MAX_BLOB_BYTES)
                throw new ArgumentOutOfRangeException(nameof(secret),
                    $"Secret exceeds the {CRED_MAX_BLOB_BYTES}-byte credential blob limit.");

            IntPtr targetPtr = Marshal.StringToCoTaskMemUni(target);
            IntPtr blobPtr = Marshal.AllocCoTaskMem(blob.Length);
            Marshal.Copy(blob, 0, blobPtr, blob.Length);
            try
            {
                var cred = new CREDENTIAL
                {
                    Type = CRED_TYPE_GENERIC,
                    TargetName = targetPtr,
                    CredentialBlobSize = blob.Length,
                    CredentialBlob = blobPtr,
                    Persist = CRED_PERSIST_LOCAL_MACHINE,
                    AttributeCount = 0,
                    UserName = IntPtr.Zero
                };

                if (!CredWrite(ref cred, 0))
                    throw new InvalidOperationException(
                        $"CredWrite failed (Win32 error {Marshal.GetLastWin32Error()}).");
            }
            finally
            {
                Marshal.FreeCoTaskMem(targetPtr);
                Marshal.FreeCoTaskMem(blobPtr);
            }
        }

        /// <summary>Returns the stored secret, or null if no credential exists for the target.</summary>
        public static string Load(string target)
        {
            if (string.IsNullOrEmpty(target)) throw new ArgumentNullException(nameof(target));

            if (!CredRead(target, CRED_TYPE_GENERIC, 0, out IntPtr credPtr))
            {
                int err = Marshal.GetLastWin32Error();
                if (err == ERROR_NOT_FOUND) return null;
                throw new InvalidOperationException($"CredRead failed (Win32 error {err}).");
            }

            try
            {
                var cred = Marshal.PtrToStructure<CREDENTIAL>(credPtr);
                if (cred.CredentialBlobSize == 0 || cred.CredentialBlob == IntPtr.Zero)
                    return string.Empty;

                byte[] blob = new byte[cred.CredentialBlobSize];
                Marshal.Copy(cred.CredentialBlob, blob, 0, cred.CredentialBlobSize);
                return Encoding.Unicode.GetString(blob);
            }
            finally
            {
                CredFree(credPtr);
            }
        }

        /// <summary>Deletes the credential. Returns false if it did not exist.</summary>
        public static bool Delete(string target)
        {
            if (string.IsNullOrEmpty(target)) throw new ArgumentNullException(nameof(target));

            if (!CredDelete(target, CRED_TYPE_GENERIC, 0))
            {
                int err = Marshal.GetLastWin32Error();
                if (err == ERROR_NOT_FOUND) return false;
                throw new InvalidOperationException($"CredDelete failed (Win32 error {err}).");
            }
            return true;
        }
    }
}
