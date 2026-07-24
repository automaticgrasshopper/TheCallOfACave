using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace TCC.Persistence
{
    /// <summary>
    /// Owns profile files and their index. Each write is flushed to a temporary
    /// file in the destination directory before an atomic move or replacement.
    /// </summary>
    public sealed class ProfileRepository
    {
        private const string IndexFileName = "index.json";
        private const string DataDirectoryName = "data";
        private const string BackupSuffix = ".backup";

        private readonly string _rootDirectory;
        private readonly string _dataDirectory;

        public static string DefaultRootDirectory =>
            Path.Combine(Application.persistentDataPath, "profiles");

        public string RootDirectory => _rootDirectory;
        public string IndexPath => Path.Combine(_rootDirectory, IndexFileName);
        public string IndexBackupPath => IndexPath + BackupSuffix;

        public ProfileRepository() : this(DefaultRootDirectory)
        {
        }

        public ProfileRepository(string rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
                throw new ArgumentException("Profile root directory cannot be blank.", nameof(rootDirectory));

            _rootDirectory = Path.GetFullPath(rootDirectory);
            _dataDirectory = Path.Combine(_rootDirectory, DataDirectoryName);
        }

        public ProfileIndex LoadIndex()
        {
            if (!File.Exists(IndexPath) && !File.Exists(IndexBackupPath))
                return ProfileIndex.CreateEmpty();

            Exception primaryException = null;
            if (TryReadIndex(IndexPath, out ProfileIndex primary, out primaryException))
                return primary;
            if (TryReadIndex(IndexBackupPath, out ProfileIndex backup, out Exception backupException))
                return backup;

            throw BuildCorruptionException("profile index", primaryException, backupException);
        }

        public ProfileLoadResult LoadProfile(string profileId)
        {
            ValidateProfileId(profileId);
            string primaryPath = GetProfilePath(profileId);
            string backupPath = GetProfileBackupPath(profileId);

            Exception primaryException = null;
            if (TryReadProfile(primaryPath, out PlayerProfile primary, out primaryException))
                return new ProfileLoadResult(primary, false, null);
            if (TryReadProfile(backupPath, out PlayerProfile backup, out Exception backupException))
                return new ProfileLoadResult(backup, true, Describe(primaryException));

            throw BuildCorruptionException($"profile {profileId}", primaryException, backupException);
        }

        public void SaveProfile(PlayerProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            profile.EnsureValid();
            EnsureDirectories();

            string profilePath = GetProfilePath(profile.ProfileId);
            AtomicWrite(
                profilePath,
                GetProfileBackupPath(profile.ProfileId),
                ProfileJson.Serialize(profile, true),
                path => TryReadProfile(path, out _, out _));

            ProfileIndex index = LoadIndex();
            index.Upsert(profile);
            SaveIndex(index);
        }

        public void SaveIndex(ProfileIndex index)
        {
            if (index == null) throw new ArgumentNullException(nameof(index));
            index.EnsureValid();
            EnsureDirectories();

            string json = JsonUtility.ToJson(index, true);
            AtomicWrite(
                IndexPath,
                IndexBackupPath,
                json,
                path => TryReadIndex(path, out _, out _));
        }

        public string GetProfilePath(string profileId)
        {
            ValidateProfileId(profileId);
            return Path.Combine(_dataDirectory, profileId + ".json");
        }

        public string GetProfileBackupPath(string profileId)
        {
            return GetProfilePath(profileId) + BackupSuffix;
        }

        private void EnsureDirectories()
        {
            Directory.CreateDirectory(_rootDirectory);
            Directory.CreateDirectory(_dataDirectory);
        }

        private static void AtomicWrite(
            string destinationPath,
            string backupPath,
            string content,
            Func<string, bool> isValidDestination)
        {
            string temporaryPath = destinationPath + ".tmp." + Guid.NewGuid().ToString("N");
            try
            {
                WriteThrough(temporaryPath, content);
                if (!File.Exists(destinationPath))
                {
                    File.Move(temporaryPath, destinationPath);
                    return;
                }

                if (isValidDestination(destinationPath))
                {
                    File.Replace(temporaryPath, destinationPath, backupPath, true);
                    return;
                }

                string corruptPath = destinationPath + ".corrupt." + Guid.NewGuid().ToString("N");
                File.Copy(destinationPath, corruptPath, false);
                File.Replace(temporaryPath, destinationPath, null, true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        private static void WriteThrough(string path, string content)
        {
            byte[] bytes = new UTF8Encoding(false).GetBytes(content);
            using (var stream = new FileStream(
                       path,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       4096,
                       FileOptions.WriteThrough))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
        }

        private static bool TryReadProfile(
            string path,
            out PlayerProfile profile,
            out Exception exception)
        {
            profile = null;
            exception = null;
            try
            {
                if (!File.Exists(path))
                    throw new FileNotFoundException("Profile file does not exist.", path);

                profile = ProfileJson.Deserialize(File.ReadAllText(path, Encoding.UTF8));
                return true;
            }
            catch (Exception caught)
            {
                exception = caught;
                return false;
            }
        }

        private static bool TryReadIndex(
            string path,
            out ProfileIndex index,
            out Exception exception)
        {
            index = null;
            exception = null;
            try
            {
                if (!File.Exists(path))
                    throw new FileNotFoundException("Profile index does not exist.", path);

                string json = File.ReadAllText(path, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json))
                    throw new InvalidDataException("Profile index is blank.");

                index = JsonUtility.FromJson<ProfileIndex>(json);
                if (index == null)
                    throw new InvalidDataException("Profile index JSON produced no model.");

                index.EnsureValid();
                return true;
            }
            catch (Exception caught)
            {
                exception = caught;
                return false;
            }
        }

        private static InvalidDataException BuildCorruptionException(
            string subject,
            Exception primaryException,
            Exception backupException)
        {
            return new InvalidDataException(
                $"Unable to load {subject}. Primary: {Describe(primaryException)} " +
                $"Backup: {Describe(backupException)}",
                backupException ?? primaryException);
        }

        private static string Describe(Exception exception)
        {
            return exception == null ? "not attempted" : exception.Message;
        }

        private static void ValidateProfileId(string profileId)
        {
            if (!Guid.TryParseExact(profileId, "N", out _))
                throw new ArgumentException("Profile ID is missing or malformed.", nameof(profileId));
        }
    }
}
