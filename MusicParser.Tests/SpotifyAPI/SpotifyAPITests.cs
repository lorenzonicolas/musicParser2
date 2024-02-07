using System.Net;
using DTO;
using Microsoft.Extensions.Configuration;
using Moq;
using musicParser.Spotify;
using musicParser.Spotify.DTOs;
using MusicParser.Utils.HttpClient;
using Newtonsoft.Json;
using Regex;

namespace MusicParser.Tests.SpotifyAPI
{
    [TestFixture]
    [Parallelizable]
    public class SpotifyAPITests
    {
        private const string BAND_NAME = "Emperor";
        private const string ALBUM_NAME = "Anthems to the Welkin at Dusk";
        Mock<IRegexUtils> regexUtils = new();
        IConfiguration config;
        Mock<IHttpClient> httpClient = new();
        private SpotifyAPIimplemen api;
        private readonly BandDTO bandDto = new() { Id = "id", Name = BAND_NAME, Genres = new List<string>() { "Black Metal" } };

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            //Arrange
            var inMemorySettings = new Dictionary<string, string> {
                {"spotifyfClientID", "client-id"},
                {"spotifyfClientSecret", "client-secret"},
            };

            config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            regexUtils.Setup(c => c.ReplaceAllSpaces(BAND_NAME)).Returns(BAND_NAME);
            regexUtils.Setup(c => c.ReplaceAllSpaces(ALBUM_NAME)).Returns(ALBUM_NAME);
            httpClient.Setup(c => c.Post(SpotifyAPIimplemen.loginUrl, It.IsAny<HttpContent>())).Returns(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(new LoginDTO() { AccessToken = "accessToken" }))
            });

            api = new SpotifyAPIimplemen(regexUtils.Object, config, httpClient.Object);
        }

        [TearDown] public void TearDown()
        {
            httpClient.Invocations.Clear();
            regexUtils.Invocations.Clear();
        }

        [Test]
        public void Constructor()
        {
            httpClient.Verify(c => c.SetAuthHeaders("Basic", It.IsAny<string>()), Times.Once);
            httpClient.Verify(c => c.SetAuthHeaders("Bearer", It.IsAny<string>()), Times.Never);
            httpClient.Verify(c => c.Post(SpotifyAPIimplemen.loginUrl, It.IsAny<HttpContent>()), Times.Once);
        }

        [Test]
        public void Constructor_Exception()
        {
            Mock<IHttpClient> client = new();

            client.Setup(c => c.Post(SpotifyAPIimplemen.loginUrl, It.IsAny<HttpContent>())).Returns(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.BadGateway
            });

            Assert.Throws<Exception>(() => new SpotifyAPIimplemen(regexUtils.Object, config, client.Object), "Communication error");
        }

        [Test]
        public void Constructor_Exception2()
        {
            Mock<IHttpClient> client = new();

            client.Setup(c => c.Post(SpotifyAPIimplemen.loginUrl, It.IsAny<HttpContent>())).Returns(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("")
            });

            Assert.Throws<Exception>(() => new SpotifyAPIimplemen(regexUtils.Object, config, client.Object), "Error getting the access token");
        }

        [Test]
        public void SearchBand()
        {
            var responseObject = new SearchBandResponse()
            {
                Artists = new BandSearchDTO()
                {
                    Items = new List<BandDTO> { bandDto },
                    Total = 1
                }
            };

            httpClient
                .Setup(c => c.Get($"https://api.spotify.com/v1/search?q={BAND_NAME}&type=artist"))
                .Returns(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(responseObject))
            });

            // Act
            var result = api.SearchBand(BAND_NAME);

            // Assert
            regexUtils.Verify(c => c.ReplaceAllSpaces(BAND_NAME), Times.Once);
            httpClient.Verify(c => c.SetAuthHeaders("Bearer", It.IsAny<string>()), Times.Once);
            httpClient.Verify(c => c.Get($"https://api.spotify.com/v1/search?q={BAND_NAME}&type=artist"), Times.Once);
            
            Assert.That(result?.Artists.Total, Is.EqualTo(1));
            Assert.That(result?.Artists.Items.Count, Is.EqualTo(1));
            Assert.That(result?.Artists.Items.First().Name, Is.EqualTo(BAND_NAME));
        }

        [Test]
        public void SearchBand_ByNameAndGenre()
        {
            var responseObject = new SearchBandResponse()
            {
                Artists = new BandSearchDTO()
                {
                    Items = new List<BandDTO> { bandDto },
                    Total = 1
                }
            };

            var genreToSearch = "Black Metal";
            var query = $"{BAND_NAME}%20genre:%22{genreToSearch}%22";
            var urlToUse = $"https://api.spotify.com/v1/search?q={query}&type=artist";

            httpClient.Setup(c => c.Get(urlToUse))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(responseObject))
                });
            regexUtils.Setup(c => c.ReplaceAllSpaces(genreToSearch)).Returns(genreToSearch);

            // Act
            var result = api.SearchBand(BAND_NAME, genreToSearch);

            // Assert
            regexUtils.Verify(c => c.ReplaceAllSpaces(BAND_NAME), Times.Once);
            httpClient.Verify(c => c.SetAuthHeaders("Bearer", It.IsAny<string>()), Times.Once);
            httpClient.Verify(c => c.Get(urlToUse), Times.Once);
            Assert.Multiple(() =>
            {
                Assert.That(result?.Artists.Total, Is.EqualTo(1));
                Assert.That(result?.Artists.Items.Count, Is.EqualTo(1));
                Assert.That(result?.Artists.Items.First().Name, Is.EqualTo(BAND_NAME));
            });
        }

        [Test]
        public void SearchBandById()
        {
            var responseObject = new BandDTO() { Id = "id", Name = BAND_NAME, Genres = new List<string>() { "Black Metal" } };

            httpClient
                .Setup(c => c.Get($"https://api.spotify.com/v1/artists/{BAND_NAME}"))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(responseObject))
                });

            // Act
            var result = api.SearchBandById(BAND_NAME);

            // Assert
            httpClient.Verify(c => c.SetAuthHeaders("Bearer", It.IsAny<string>()), Times.Once);
            httpClient.Verify(c => c.Get($"https://api.spotify.com/v1/artists/{BAND_NAME}"), Times.Once);
            Assert.Multiple(() =>
            {
                Assert.That(result?.Id, Is.EqualTo("id"));
                Assert.That(result?.Name, Is.EqualTo(BAND_NAME));
            });
        }

        [Test]
        public void SearchAlbum()
        {
            var responseObject = new SearchAlbumResponse()
            {
                Albums = new AlbumSearchDTO()
                {
                    Total = 1,
                    Items =
                    [
                        new AlbumDTO()
                        {
                            Artists = new List<BandDTO>(){ bandDto },
                            Id = "albumId",
                            Name = ALBUM_NAME,
                            ReleaseDate = DateTime.Now.AddMonths(-10).ToShortDateString(),
                            AlbumType = AlbumType.FullAlbum.ToString()
                        }
                    ]
                }
            };
            
            var urlToUse = $"https://api.spotify.com/v1/search?q={ALBUM_NAME}&type=album";

            httpClient.Setup(c => c.Get(urlToUse))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(responseObject))
                });

            // Act
            var result = api.SearchAlbum(ALBUM_NAME);

            // Assert
            httpClient.Verify(c => c.SetAuthHeaders("Bearer", It.IsAny<string>()), Times.Once);
            httpClient.Verify(c => c.Get(urlToUse), Times.Once);
            regexUtils.Verify(c => c.ReplaceAllSpaces(ALBUM_NAME), Times.Once);
            Assert.Multiple(() =>
            {
                Assert.That(result?.Albums.Total, Is.EqualTo(1));
                Assert.That(result?.Albums.Items.Count, Is.EqualTo(1));
                Assert.That(result?.Albums.Items.First().Name, Is.EqualTo(ALBUM_NAME));
                Assert.That(result?.Albums.Items.First().Artists.First().Name, Is.EqualTo(BAND_NAME));
            });
        }

        [Test]
        public void SearchAlbum_ByNameAndBand()
        {
            var responseObject = new SearchAlbumResponse()
            {
                Albums = new AlbumSearchDTO()
                {
                    Total = 1,
                    Items = new List<AlbumDTO>()
                    {
                        new AlbumDTO()
                        {
                            Artists = new List<BandDTO>(){ bandDto },
                            Id = "albumId",
                            Name = ALBUM_NAME,
                            ReleaseDate = DateTime.Now.AddMonths(-10).ToShortDateString(),
                            AlbumType = AlbumType.FullAlbum.ToString()
                        }
                    }
                }
            };
            var query = $"album:{ALBUM_NAME}%20artist:{BAND_NAME}";
            var urlToUse = $"https://api.spotify.com/v1/search?q={query}&type=album";

            httpClient.Setup(c => c.Get(urlToUse))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(responseObject))
                });

            // Act
            var result = api.SearchAlbum(ALBUM_NAME, BAND_NAME);

            // Assert
            httpClient.Verify(c => c.SetAuthHeaders("Bearer", It.IsAny<string>()), Times.Once);
            httpClient.Verify(c => c.Get(urlToUse), Times.Once);
            regexUtils.Verify(c => c.ReplaceAllSpaces(ALBUM_NAME), Times.Once);
            Assert.Multiple(() =>
            {
                Assert.That(result?.Albums.Total, Is.EqualTo(1));
                Assert.That(result?.Albums.Items.Count, Is.EqualTo(1));
                Assert.That(result?.Albums.Items.First().Name, Is.EqualTo(ALBUM_NAME));
                Assert.That(result?.Albums.Items.First().Artists.First().Name, Is.EqualTo(BAND_NAME));
            });
        }
    }
}
