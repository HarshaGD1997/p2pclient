using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using p2pclient.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace p2pclient.Pages
{
    public class FilesModel : PageModel
    {
        private readonly HttpClient _httpClient;

        public FilesModel(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [BindProperty]
        public List<FileMetadata> Files { get; set; }

        [BindProperty]
        public string ServerUrl { get; set; }

        [BindProperty]
        public int FileId { get; set; }

        [BindProperty]
        public IFormFile Upload { get; set; }

        [BindProperty]
        public string ErrorMessage { get; set; }

        public async Task OnGet()
        {
            ServerUrl = "http://localhost:5117"; // Default server URL
            await LoadFiles();
        }

        public async Task<IActionResult> OnPostDownloadAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{ServerUrl}/api/files/{FileId}");
                if (response.IsSuccessStatusCode)
                {
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? $"file_{FileId}";

                    return File(fileBytes, "application/octet-stream", fileName);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    ErrorMessage = "File not found.";
                }
                else
                {
                    ErrorMessage = "Failed to download file.";
                }
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = $"Error downloading file: {ex.Message}";
            }
            await LoadFiles();
            return Page();
        }

        public async Task<IActionResult> OnPostUploadAsync()
        {
            if (Upload != null && Upload.Length > 0)
            {
                try
                {
                    using (var content = new MultipartFormDataContent())
                    {
                        var fileStream = Upload.OpenReadStream();
                        var streamContent = new StreamContent(fileStream);
                        content.Add(streamContent, "file", Upload.FileName);

                        var response = await _httpClient.PostAsync($"{ServerUrl}/api/files", content);
                        if (response.IsSuccessStatusCode)
                        {
                            return RedirectToPage();
                        }

                        ErrorMessage = "Failed to upload file.";
                    }
                }
                catch (HttpRequestException ex)
                {
                    ErrorMessage = $"Error uploading file: {ex.Message}";
                }
            }
            else
            {
                ErrorMessage = "No file selected for upload.";
            }

            await LoadFiles();
            return Page();
        }

        private async Task LoadFiles()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{ServerUrl}/api/files");
                if (response.IsSuccessStatusCode)
                {
                    Files = await response.Content.ReadFromJsonAsync<List<FileMetadata>>();
                }
                else
                {
                    Files = new List<FileMetadata>();
                    ErrorMessage = "Failed to load files.";
                }
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = $"Error loading files: {ex.Message}";
                Files = new List<FileMetadata>();
            }
        }
    }
}
