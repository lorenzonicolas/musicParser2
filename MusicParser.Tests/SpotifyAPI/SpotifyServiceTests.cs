using Moq;
using musicParser.Spotify;
using musicParser.Spotify.DTOs;
using musicParser.Utils.Loggers;
using MusicParser.Utils.HttpClient;
using NUnit.Framework.Internal;

namespace MusicParser.Tests.SpotifyAPI
{
    [TestFixture]
    [Parallelizable]
    public class SpotifyServiceTests
    {
        const string albumName = "album_name";
        const string bandName = "band_name";
        private readonly Mock<IExecutionLogger> logger = new();
        private readonly Mock<IHttpClient> http = new();

        [Test]
        public void GetAlbumYear_Null()
        {
            Mock<ISpotifyAPI> api = new();
            api.Setup(o => o.SearchAlbum(albumName, bandName)).Returns<SearchAlbumResponse>(null);

            var service = new SpotifyService(logger.Object, api.Object, http.Object);

            var result = service.GetAlbumYear(albumName, bandName);

            Assert.That(result, Is.Null);
            api.Verify(o => o.SearchAlbum(albumName, bandName), Times.Once);

            api.Reset();
        }

        [Test]
        public void GetAlbumYear_MultipleResultsReturnNull()
        {
            Mock<ISpotifyAPI> api = new();
            var response = new SearchAlbumResponse()
            {
                Albums = new AlbumSearchDTO
                {
                    Total = 2
                }
            };

            api.Setup(o => o.SearchAlbum(albumName, bandName)).Returns(response);

            var service = new SpotifyService(logger.Object, api.Object, http.Object);

            var result = service.GetAlbumYear(albumName, bandName);

            Assert.That(result, Is.Null);
            api.Verify(o => o.SearchAlbum(albumName, bandName), Times.Once);

            api.Reset();
        }

        [Test]
        public void GetAlbumYear_MultipleArtistsReturnNull()
        {
            Mock<ISpotifyAPI> api = new();
            var response = new SearchAlbumResponse()
            {
                Albums = new AlbumSearchDTO
                {
                    Total = 2,
                    Items = new List<AlbumDTO> { new AlbumDTO { Artists = new List<BandDTO>() { new BandDTO(), new BandDTO()} } }
                }
            };

            api.Setup(o => o.SearchAlbum(albumName, bandName)).Returns(response);

            var service = new SpotifyService(logger.Object, api.Object, http.Object);

            var result = service.GetAlbumYear(albumName, bandName);

            Assert.That(result, Is.Null);
            api.Verify(o => o.SearchAlbum(albumName, bandName), Times.Once);

            api.Reset();
        }

        [Test]
        public void GetAlbumYear_InvalidDate()
        {
            Mock<ISpotifyAPI> api = new();
            var response = new SearchAlbumResponse()
            {
                Albums = new AlbumSearchDTO
                {
                    Total = 1,
                    Items = new List<AlbumDTO>()
                    {
                        new AlbumDTO()
                        {
                            Name = albumName,
                            ReleaseDate = "INVALID_DATE"
                        }
                    }
                }
            };

            api.Setup(o => o.SearchAlbum(albumName, bandName)).Returns(response);

            var service = new SpotifyService(logger.Object, api.Object, http.Object);

            var result = service.GetAlbumYear(albumName, bandName);

            Assert.That(result, Is.Null);
            api.Verify(o => o.SearchAlbum(albumName, bandName), Times.Once);

            api.Reset();
        }

        [Test]
        public void GetAlbumYear_Ok()
        {
            Mock<ISpotifyAPI> api = new();
            var response = new SearchAlbumResponse()
            {
                Albums = new AlbumSearchDTO
                {
                    Total = 1,
                    Items = new List<AlbumDTO>()
                    {
                        new AlbumDTO()
                        {
                            Name = albumName,
                            ReleaseDate = "2022-01-01",
                            Artists = new List<BandDTO>{ new BandDTO() { } }
                        }
                    }
                }
            };

            api.Setup(o => o.SearchAlbum(albumName, bandName)).Returns(response);

            var service = new SpotifyService(logger.Object, api.Object, http.Object);

            var result = service.GetAlbumYear(albumName, bandName);

            Assert.That(result, Is.EqualTo("2022"));
            api.Verify(o => o.SearchAlbum(albumName, bandName), Times.Once);

            api.Reset();
        }

        [Test]
        public void GetArtistGenre_Null()
        {
            Mock<ISpotifyAPI> api = new();

            api.Setup(o => o.SearchBand(bandName)).Returns<SearchBandResponse>(null);

            var service = new SpotifyService(logger.Object, api.Object, http.Object);

            var result = service.GetArtistGenre(bandName);

            Assert.That(result, Is.Null);
            api.Verify(o => o.SearchBand(bandName), Times.Once);

            api.Reset();
        }

        [Test]
        public void GetArtistGenre_Exception()
        {
            Mock<ISpotifyAPI> api = new();

            api.Setup(o => o.SearchBand(bandName)).Throws<Exception>();

            var service = new SpotifyService(logger.Object, api.Object, http.Object);

            var result = service.GetArtistGenre(bandName);

            Assert.That(result, Is.Null);
            api.Verify(o => o.SearchBand(bandName), Times.Once);

            api.Reset();
        }

        [Test]
        public void GetArtistGenre_MultipleResults()
        {
            Mock<ISpotifyAPI> api = new();
            var response = new SearchBandResponse()
            {
                Artists = new BandSearchDTO()
                {
                    Total = 2
                }
            };

            api.Setup(o => o.SearchBand(bandName)).Returns(response);

            var service = new SpotifyService(logger.Object, api.Object, http.Object);

            var result = service.GetArtistGenre(bandName);

            Assert.That(result, Is.Null);
            api.Verify(o => o.SearchBand(bandName), Times.Once);

            api.Reset();
        }

        [Test]
        public void GetArtistGenre_Ok()
        {
            Mock<ISpotifyAPI> api = new();
            var response = new SearchBandResponse()
            {
                Artists = new BandSearchDTO()
                {
                    Total = 1,
                    Items = new List<BandDTO>()
                    {
                        new BandDTO()
                        {
                            Genres = new List<string>(){"Black Metal"}
                        }
                    }
                }
            };

            api.Setup(o => o.SearchBand(bandName)).Returns(response);

            var service = new SpotifyService(logger.Object, api.Object, http.Object);

            var result = service.GetArtistGenre(bandName);

            Assert.That(result, Does.Contain("Black Metal"));
            api.Verify(o => o.SearchBand(bandName), Times.Once);

            api.Reset();
        }

        [Test]
        public void GetArtistGenreUsingAlbum_Ok()
        {
            var mockBandDto = new BandDTO() { Id = "bandId", Genres = new List<string>() { "Black Metal" } };
            Mock<ISpotifyAPI> api = new();
            var response = new SearchAlbumResponse()
            {
                Albums = new AlbumSearchDTO
                {
                    Total = 1,
                    Items = new List<AlbumDTO>()
                    {
                        new AlbumDTO()
                        {
                            Name = albumName,
                            ReleaseDate = "2022-01-01",
                            Artists = new List<BandDTO>{mockBandDto}
                        }
                    }
                }
            };

            api.Setup(o => o.SearchAlbum(albumName, bandName)).Returns(response);
            api.Setup(o => o.SearchBandById("bandId")).Returns(mockBandDto);

            var service = new SpotifyService(logger.Object, api.Object, http.Object);

            var result = service.GetArtistGenreUsingAlbum(bandName, albumName);

            Assert.That(result, Does.Contain("Black Metal"));
            api.Verify(o => o.SearchAlbum(albumName, bandName), Times.Once);
            api.Verify(o => o.SearchBandById("bandId"), Times.Once);

            api.Reset();
        }

        [Test]
        public void GetArtistGenreUsingAlbum_ThrowsException()
        {
            Mock<ISpotifyAPI> api = new();
            
            api.Setup(o => o.SearchAlbum(albumName, bandName)).Throws<Exception>();

            var service = new SpotifyService(logger.Object, api.Object, http.Object);

            var result = service.GetArtistGenreUsingAlbum(bandName, albumName);

            Assert.That(result, Is.Null);
            api.Verify(o => o.SearchAlbum(albumName, bandName), Times.Once);
            api.Verify(o => o.SearchBandById("bandId"), Times.Never);

            api.Reset();
        }

        [Test]
        public void GetArtistGenreUsingAlbum_ByBand()
        {
            Mock<ISpotifyAPI> api = new();

            var response = new SearchAlbumResponse()
            {
                Albums = new AlbumSearchDTO { Total = 0 }
            };

            var bandResponse = new SearchBandResponse()
            {
                Artists = new BandSearchDTO()
                {
                    Total = 1,
                    Items = new List<BandDTO>()
                    {
                        new BandDTO() { Genres = new List<string>(){"Black Metal"} }
                    }
                }
            };

            api.Setup(o => o.SearchBand(bandName)).Returns(bandResponse);
            api.Setup(o => o.SearchAlbum(albumName, bandName)).Returns(response);

            var service = new SpotifyService(logger.Object, api.Object, http.Object);

            var result = service.GetArtistGenreUsingAlbum(bandName, albumName);

            Assert.That(result, Does.Contain("Black Metal"));
            api.Verify(o => o.SearchAlbum(albumName, bandName), Times.Once);
            api.Verify(o => o.SearchBand(bandName), Times.Once);
            api.Verify(o => o.SearchBandById("bandId"), Times.Never);

            api.Reset();
        }

        [Test]
        public void DownloadAlbumCover_Exception()
        {
            Mock<ISpotifyAPI> api = new();

            api.Setup(o => o.SearchAlbum(albumName, bandName)).Throws<Exception>();

            var service = new SpotifyService(logger.Object, api.Object, http.Object);

            var result = service.DownloadAlbumCover(bandName, albumName);

            Assert.That(result, Is.Null);
            api.Verify(o => o.SearchAlbum(albumName, bandName), Times.Once);

            api.Reset();
        }

        [Test]
        public void DownloadAlbumCover_NoResults()
        {
            Mock<ISpotifyAPI> api = new();

            api.Setup(o => o.SearchAlbum(albumName, bandName)).Returns<SearchAlbumResponse>(null);

            var service = new SpotifyService(logger.Object, api.Object, http.Object);

            var result = service.DownloadAlbumCover(bandName, albumName);

            Assert.That(result, Is.Null);
            api.Verify(o => o.SearchAlbum(albumName, bandName), Times.Once);

            api.Reset();
        }

        [Test]
        public void DownloadAlbumCover_EmptyImages()
        {
            Mock<ISpotifyAPI> api = new();

            var response = new SearchAlbumResponse()
            {
                Albums = new AlbumSearchDTO
                {
                    Total = 1,
                    Items = new List<AlbumDTO>()
                    {
                        new AlbumDTO()
                        {
                            Images = new List<AlbumImageDTO>()
                        }
                    }
                }
            };

            api.Setup(o => o.SearchAlbum(albumName, bandName)).Returns(response);

            var service = new SpotifyService(logger.Object, api.Object, http.Object);

            var result = service.DownloadAlbumCover(bandName, albumName);

            Assert.That(result, Is.Null);
            api.Verify(o => o.SearchAlbum(albumName, bandName), Times.Once);

            api.Reset();
        }

        [Test]
        public void DownloadAlbumCover_Ok()
        {
            Mock<ISpotifyAPI> api = new();

            var response = new SearchAlbumResponse()
            {
                Albums = new AlbumSearchDTO
                {
                    Total = 1,
                    Items = new List<AlbumDTO>()
                    {
                        new AlbumDTO()
                        {
                            Artists = new List<BandDTO>() { new BandDTO() },
                            Images = new List<AlbumImageDTO>() { new AlbumImageDTO() { Url = "http://www.test.com" } }
                        }
                    }
                }
            };

            var mockedImage = new List<byte>().ToArray();
            http.Setup(o => o.GetByteArrayAsync(new Uri("http://www.test.com"))).ReturnsAsync(mockedImage);
            api.Setup(o => o.SearchAlbum(albumName, bandName)).Returns(response);

            var service = new SpotifyService(logger.Object, api.Object, http.Object);

            var result = service.DownloadAlbumCover(bandName, albumName);

            Assert.That(result, Is.EqualTo(mockedImage));
            api.Verify(o => o.SearchAlbum(albumName, bandName), Times.Once);
            http.Verify(o => o.GetByteArrayAsync(new Uri("http://www.test.com")), Times.Once);

            api.Reset();
        }
    }
}
