using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace SS14.Launcher
{
    internal static class Helpers
    {
        public static void ExtractZipToDirectory(string directory, Stream zipStream)
        {
            using (var zipArchive = new ZipArchive(zipStream))
            {
                zipArchive.ExtractToDirectory(directory);
            }
        }

        public static void ClearDirectory(string directory)
        {
            var dirInfo = new DirectoryInfo(directory);
            foreach (var fileInfo in dirInfo.EnumerateFiles())
            {
                fileInfo.Delete();
            }

            foreach (var childDirInfo in dirInfo.EnumerateDirectories())
            {
                childDirInfo.Delete(true);
            }
        }

        public static async Task DownloadToFile(this HttpClient client, Uri uri, string filePath,
            Action<float> progress = null)
        {
            await Task.Run(async () =>
            {
                using (var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = File.OpenWrite(filePath))
                    {
                        var totalLength = response.Content.Headers.ContentLength;
                        if (totalLength.HasValue)
                        {
                            progress?.Invoke(0);
                        }

                        var totalRead = 0L;
                        var reads = 0L;
                        const int bufferLength = 8192;
                        var buffer = ArrayPool<byte>.Shared.Rent(bufferLength);
                        var isMoreToRead = true;

                        do
                        {
                            var read = await contentStream.ReadAsync(buffer, 0, bufferLength);
                            if (read == 0)
                            {
                                isMoreToRead = false;
                            }
                            else
                            {
                                await fileStream.WriteAsync(buffer, 0, read);

                                reads += 1;
                                totalRead += read;
                                if (totalLength.HasValue && reads % 20 == 0)
                                {
                                    progress?.Invoke(totalRead / (float) totalLength.Value);
                                }
                            }
                        } while (isMoreToRead);
                    }
                }
            });
        }
    }
}
