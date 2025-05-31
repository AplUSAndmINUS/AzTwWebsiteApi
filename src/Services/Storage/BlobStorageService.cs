using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AzTwWebsiteApi.Utils;
using AzTwWebsiteApi.Models.Blog;

namespace AzTwWebsiteApi.Services.Storage
{
    public class BlobStorageService<T> : IBlobStorageService<T> where T : class
    {
        private readonly BlobContainerClient _containerClient;
        private readonly ILogger<BlobStorageService<T>> _logger;

        public BlobStorageService(
            string connectionString,
            string containerName,
            ILogger<BlobStorageService<T>> logger)
        {
            _logger = logger;
            var blobServiceClient = new BlobServiceClient(connectionString);
            _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            _containerClient.CreateIfNotExists();
        }

        public async Task<T?> GetBlobAsync(string blobName)
        {
            try
            {
                var blobClient = _containerClient.GetBlobClient(blobName);

                if (!await blobClient.ExistsAsync())
                {
                    _logger.LogWarning("Blob {BlobName} not found", blobName);
                    return null;
                }

                var response = await blobClient.DownloadContentAsync();
                var content = response.Value.Content.ToString();
                return JsonSerializer.Deserialize<T>(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving blob {BlobName}", blobName);
                throw;
            }
        }

        public async Task<IEnumerable<T>> GetAllBlobsAsync()
        {
            try
            {
                var blobs = new List<T>();

                await foreach (var blobItem in _containerClient.GetBlobsAsync())
                {
                    var blobClient = _containerClient.GetBlobClient(blobItem.Name);
                    var response = await blobClient.DownloadContentAsync();
                    var content = response.Value.Content.ToString();
                    var deserialized = JsonSerializer.Deserialize<T>(content);
                    if (deserialized != null)
                    {
                        blobs.Add(deserialized);
                    }
                }

                return blobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all blobs");
                throw;
            }
        }

        public async Task<T> UploadBlobAsync(string blobName, T data, IDictionary<string, string>? metadata = null)
        {
            try
            {
                var blobClient = _containerClient.GetBlobClient(blobName);
                var content = JsonSerializer.Serialize(data);
                var options = new BlobUploadOptions();
                
                if (metadata != null)
                {
                    options.Metadata = metadata;
                }
                
                await blobClient.UploadAsync(BinaryData.FromString(content), options);
                _logger.LogInformation("Successfully uploaded blob {BlobName}", blobName);
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading blob {BlobName}", blobName);
                throw;
            }
        }

        public async Task<T> UpdateBlobAsync(string blobName, T data, IDictionary<string, string>? metadata = null)
        {
            try
            {
                var blobClient = _containerClient.GetBlobClient(blobName);

                if (!await blobClient.ExistsAsync())
                {
                    _logger.LogWarning("Blob {BlobName} not found for update", blobName);
                    throw new InvalidOperationException($"Blob {blobName} does not exist");
                }

                var content = JsonSerializer.Serialize(data);
                var options = new BlobUploadOptions();
                
                if (metadata != null)
                {
                    options.Metadata = metadata;
                }

                await blobClient.UploadAsync(BinaryData.FromString(content), options);
                _logger.LogInformation("Successfully updated blob {BlobName}", blobName);
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating blob {BlobName}", blobName);
                throw;
            }
        }

        public async Task DeleteBlobAsync(string blobName)
        {
            try
            {
                var blobClient = _containerClient.GetBlobClient(blobName);

                if (await blobClient.ExistsAsync())
                {
                    await blobClient.DeleteAsync();
                    _logger.LogInformation("Successfully deleted blob {BlobName}", blobName);
                }
                else
                {
                    _logger.LogWarning("Blob {BlobName} not found for deletion", blobName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting blob {BlobName}", blobName);
                throw;
            }
        }

        public async Task<(IEnumerable<T> Items, string? ContinuationToken)> GetPagedBlobsAsync(
            int maxResults,
            string? continuationToken = null)
        {
            try
            {
                var blobs = new List<T>();
                var options = new BlobsOptions
                {
                    PageSize = maxResults
                };

                var resultSegment = _containerClient.GetBlobsAsync(options)
                    .AsPages(continuationToken, maxResults);

                await foreach (var blobPage in resultSegment)
                {
                    foreach (var blobItem in blobPage.Values)
                    {
                        var blobClient = _containerClient.GetBlobClient(blobItem.Name);
                        var response = await blobClient.DownloadContentAsync();
                        var content = response.Value.Content.ToString();
                        var deserialized = JsonSerializer.Deserialize<T>(content);
                        if (deserialized != null)
                        {
                            blobs.Add(deserialized);
                        }
                    }

                    return (blobs, blobPage.ContinuationToken);
                }

                return (blobs, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paged blobs");
                throw;
            }
        }
    }
}
//                 var blobClient = containerClient.GetBlobClient($"{id}.json");

//                 if (!await blobClient.ExistsAsync())
//                 {
//                     _logger.LogWarning("Blog post with ID {Id} not found", id);
//                     return null;
//                 }

//                 var response = await blobClient.DownloadContentAsync();
//                 var content = response.Value.Content.ToString();
//                 return JsonSerializer.Deserialize<BlogPost>(content);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error retrieving blog post with ID {Id}", id);
//                 throw;
//             }
//         }

//         public async Task<IEnumerable<BlogPost>> GetBlogPostsAsync()
//         {
//             try
//             {
//                 var containerClient = _blobServiceClient.GetBlobContainerClient(Constants.BlobContainers.BlogPosts);
//                 var posts = new List<BlogPost>();

//                 await foreach (var blobItem in containerClient.GetBlobsAsync())
//                 {
//                     var blobClient = containerClient.GetBlobClient(blobItem.Name);
//                     var response = await blobClient.DownloadContentAsync();
//                     var content = response.Value.Content.ToString();
//                     posts.Add(JsonSerializer.Deserialize<BlogPost>(content));
//                 }

//                 return posts;
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error retrieving blog posts");
//                 throw;
//             }
//         }

//         public async Task<BlogImage> GetBlogImageAsync(string id)
//         {
//             try
//             {
//                 var containerClient = _blobServiceClient.GetBlobContainerClient(Constants.BlobContainers.BlogImages);
//                 var blobClient = containerClient.GetBlobClient(id);

//                 if (!await blobClient.ExistsAsync())
//                 {
//                     _logger.LogWarning("Blog image with ID {Id} not found", id);
//                     return null;
//                 }

//                 var properties = await blobClient.GetPropertiesAsync();
                
//                 return new BlogImage
//                 {
//                     Id = id,
//                     FileName = Path.GetFileName(blobClient.Name),
//                     ContentType = properties.Value.ContentType,
//                     FileSize = properties.Value.ContentLength,
//                     Url = blobClient.Uri.ToString(),
//                     UploadDate = properties.Value.LastModified.DateTime,
//                     BlogPostId = properties.Value.Metadata.TryGetValue("BlogPostId", out var postId) ? postId : null,
//                     AltText = properties.Value.Metadata.TryGetValue("AltText", out var altText) ? altText : null,
//                     Caption = properties.Value.Metadata.TryGetValue("Caption", out var caption) ? caption : null
//                 };
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error retrieving blog image with ID {Id}", id);
//                 throw;
//             }
//         }

//         public async Task SetBlogPostAsync(BlogPost post)
//         {
//             try
//             {
//                 var containerClient = _blobServiceClient.GetBlobContainerClient(Constants.BlobContainers.BlogPosts);
//                 await containerClient.CreateIfNotExistsAsync();

//                 var blobClient = containerClient.GetBlobClient($"{post.Id}.json");
//                 var content = JsonSerializer.Serialize(post);
                
//                 await blobClient.UploadAsync(BinaryData.FromString(content), overwrite: true);
                
//                 _logger.LogInformation("Successfully uploaded blog post with ID {Id}", post.Id);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error setting blog post with ID {Id}", post.Id);
//                 throw;
//             }
//         }

//         public async Task SetBlogImageAsync(string id, Stream imageStream, string contentType, Dictionary<string, string> metadata)
//         {
//             try
//             {
//                 var containerClient = _blobServiceClient.GetBlobContainerClient(Constants.BlobContainers.BlogImages);
//                 await containerClient.CreateIfNotExistsAsync();

//                 var blobClient = containerClient.GetBlobClient(id);
                
//                 var options = new BlobUploadOptions
//                 {
//                     Metadata = metadata,
//                     HttpHeaders = new BlobHttpHeaders
//                     {
//                         ContentType = contentType
//                     }
//                 };

//                 await blobClient.UploadAsync(imageStream, options);
                
//                 _logger.LogInformation("Successfully uploaded blog image with ID {Id}", id);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error setting blog image with ID {Id}", id);
//                 throw;
//             }
//         }
//     }
// }