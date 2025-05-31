# AzTwWebsiteApi README.md

# Blog Reader Function

This project is an Azure Function application designed to read mock blog posts and images from Azure Storage using a managed identity. The application prepares the data for display in a static web app without writing any data back to the storage.

## Project Structure

- **src/Functions**: Contains the Azure Functions for retrieving blog posts and images.
  - `GetBlogPostsFunction.cs`: Function to retrieve blog posts.
  - `GetBlogImageFunction.cs`: Function to retrieve blog images.
  
- **src/Models**: Contains the data models for the blog.
  - `BlogPost.cs`: Defines the structure of a blog post.
  - `BlogImage.cs`: Defines the structure of a blog image.
  
- **src/Services**: Contains services for accessing Azure Blob Storage.
  - `IBlobStorageService.cs`: Interface for the BlobStorageService.
  - `BlobStorageService.cs`: Implementation of the IBlobStorageService.
  
- **src/Utils**: Contains utility classes for configuration.
  - `ManagedIdentityConfig.cs`: Configuration for using managed identity.

- **test/AzTwWebsiteApi.Tests**: Contains unit tests for the functions.
  - `GetBlogPostsFunctionTests.cs`: Tests for the GetBlogPostsFunction.
  - `GetBlogImageFunctionTests.cs`: Tests for the GetBlogImageFunction.

## Setup Instructions

1. Clone the repository.
2. Navigate to the project directory.
3. Install the necessary dependencies.
4. Configure your Azure Storage account and set up managed identity.
5. Run the Azure Functions locally using the Azure Functions Core Tools.

## Usage

- The `GetBlogPostsFunction` retrieves a list of blog posts from Azure Storage.
- The `GetBlogImageFunction` retrieves images associated with the blog posts.

## License

This project is licensed under the MIT License.