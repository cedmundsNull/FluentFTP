﻿using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Client.Modules;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Creates a directory on the server. If the preceding
		/// directories do not exist, then they are created.
		/// </summary>
		/// <param name="path">The full or relative path to the new remote directory</param>
		public bool CreateDirectory(string path) {
			return CreateDirectory(path, true);
		}

		/// <summary>
		/// Creates a directory on the server
		/// </summary>
		/// <param name="path">The full or relative path to the new remote directory</param>
		/// <param name="force">Try to force all non-existent pieces of the path to be created</param>
		/// <returns>True if directory was created, false if it was skipped</returns>
		public bool CreateDirectory(string path, bool force) {
			// don't verify args as blank/null path is OK
			//if (path.IsBlank())
			//	throw new ArgumentException("Required parameter is null or blank.", "path");

			path = path.GetFtpPath();

			LogFunc(nameof(CreateDirectory), new object[] { path, force });

			FtpReply reply;

			// cannot create root or working directory
			if (path.IsFtpRootDirectory()) {
				return false;
			}

			lock (m_lock) {

				// server-specific directory creation
				// ask the server handler to create a directory
				if (ServerHandler != null) {
					if (ServerHandler.CreateDirectory(this, path, path, force)) {
						return true;
					}
				}

				path = path.TrimEnd('/');

				if (force && !DirectoryExists(path.GetFtpDirectoryName())) {
					LogStatus(FtpTraceLevel.Verbose, "Create non-existent parent directory: " + path.GetFtpDirectoryName());
					CreateDirectory(path.GetFtpDirectoryName(), true);
				}

				// fix: improve performance by skipping the directory exists check
				/*else if (DirectoryExists(path)) {
					return false;
				}*/

				LogStatus(FtpTraceLevel.Verbose, "CreateDirectory " + path);

				if (!(reply = Execute("MKD " + path)).Success) {

					// if the error indicates the directory already exists, its not an error
					if (reply.Code == "550") {
						return false;
					}
					if (reply.Code[0] == '5' && reply.Message.IsKnownError(ServerStringModule.folderExists)) {
						return false;
					}

					throw new FtpCommandException(reply);
				}
				return true;

			}
		}

#if ASYNC
		/// <summary>
		/// Creates a remote directory asynchronously
		/// </summary>
		/// <param name="path">The full or relative path to the new remote directory</param>
		/// <param name="force">Try to create the whole path if the preceding directories do not exist</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>True if directory was created, false if it was skipped</returns>
		public async Task<bool> CreateDirectoryAsync(string path, bool force, CancellationToken token = default(CancellationToken)) {
			// don't verify args as blank/null path is OK
			//if (path.IsBlank())
			//	throw new ArgumentException("Required parameter is null or blank.", "path");

			path = path.GetFtpPath();

			LogFunc(nameof(CreateDirectoryAsync), new object[] { path, force });

			FtpReply reply;

			// cannot create root or working directory
			if (path.IsFtpRootDirectory()) {
				return false;
			}

			// server-specific directory creation
			// ask the server handler to create a directory
			if (ServerHandler != null) {
				if (await ServerHandler.CreateDirectoryAsync(this, path, path, force, token)) {
					return true;
				}
			}

			path = path.TrimEnd('/');

			if (force && !await DirectoryExistsAsync(path.GetFtpDirectoryName(), token)) {
				LogStatus(FtpTraceLevel.Verbose, "Create non-existent parent directory: " + path.GetFtpDirectoryName());
				await CreateDirectoryAsync(path.GetFtpDirectoryName(), true, token);
			}

			// fix: improve performance by skipping the directory exists check
			/*else if (await DirectoryExistsAsync(path, token)) {
				return false;
			}*/

			LogStatus(FtpTraceLevel.Verbose, "CreateDirectory " + path);

			if (!(reply = await ExecuteAsync("MKD " + path, token)).Success) {

				// if the error indicates the directory already exists, its not an error
				if (reply.Code == "550") {
					return false;
				}
				if (reply.Code[0] == '5' && reply.Message.IsKnownError(ServerStringModule.folderExists)) {
					return false;
				}

				throw new FtpCommandException(reply);
			}
			return true;
		}

		/// <summary>
		/// Creates a remote directory asynchronously. If the preceding
		/// directories do not exist, then they are created.
		/// </summary>
		/// <param name="path">The full or relative path to the new remote directory</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		public Task<bool> CreateDirectoryAsync(string path, CancellationToken token = default(CancellationToken)) {
			return CreateDirectoryAsync(path, true, token);
		}
#endif
	}
}