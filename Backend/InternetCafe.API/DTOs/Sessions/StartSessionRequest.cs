using System.ComponentModel.DataAnnotations;

namespace InternetCafe.API.DTOs.Sessions;

public sealed record StartSessionRequest(
    [Range(1, int.MaxValue)] int Computer_ID,
    [Range(1, int.MaxValue)] int? Customer_ID = null);
