﻿using GitCommands.UserRepositoryHistory;
using GitExtensions.Extensibility.Git;

namespace GitCommands
{
    /// <summary>
    /// Provides the ability to generate application title.
    /// </summary>
    public interface IAppTitleGenerator
    {
        /// <summary>
        /// Generates main window title according to given repository.
        /// </summary>
        /// <param name="workingDir">Path to repository.</param>
        /// <param name="isValidWorkingDir">Indicates whether the given path contains a valid repository.</param>
        /// <param name="branchName">Current branch name.</param>
        /// <param name="defaultBranchName">Default branch name if <paramref name="branchName"/> is null (but not empty).</param>
        /// <param name="pathName">Current pathfilter.</param>
        string Generate(string? workingDir = null, bool isValidWorkingDir = false, string? branchName = null, string defaultBranchName = "", string? pathName = null);
    }

    /// <summary>
    /// Generates application title.
    /// </summary>
    public sealed class AppTitleGenerator : IAppTitleGenerator
    {
        private readonly IRepositoryDescriptionProvider _descriptionProvider;
        private static string? _extraInfo;

        public AppTitleGenerator(IRepositoryDescriptionProvider descriptionProvider)
        {
            _descriptionProvider = descriptionProvider;
        }

        /// <inheritdoc />
        public string Generate(string? workingDir = null, bool isValidWorkingDir = false, string? branchName = null, string defaultBranchName = "", string? pathName = null)
        {
            if (string.IsNullOrWhiteSpace(workingDir) || !isValidWorkingDir)
            {
                return AppSettings.ApplicationName;
            }

            if (string.IsNullOrWhiteSpace(branchName))
            {
                branchName = defaultBranchName;
            }

            // Pathname normally have quotes already
            pathName = GetFileName(pathName);

            string description = _descriptionProvider.Get(workingDir);

            return $"{pathName}{description} ({branchName}) - {AppSettings.ApplicationName}{_extraInfo}";

            static string? GetFileName(string? path)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    return null;
                }

                string filePart = Path.GetFileName(path.Trim('"')).QuoteNE();
                if (string.IsNullOrWhiteSpace(filePart))
                {
                    // No file, just quote the pathFilter
                    filePart = path.StartsWith(@"""") && path.EndsWith(@"""")
                        ? path
                        : $"{path.Quote()}";
                }

                return $"{filePart} ";
            }
        }

        public static void Initialise(string sha, string buildBranch)
        {
            if (ObjectId.TryParse(sha, out ObjectId? objectId))
            {
                _extraInfo = $" {objectId.ToShortString()}";
#if DEBUG
                if (!string.IsNullOrWhiteSpace(buildBranch))
                {
                    _extraInfo += $" ({buildBranch})";
                }
            }
            else
            {
                _extraInfo = " [DEBUG]";
#endif
            }
        }
    }
}
