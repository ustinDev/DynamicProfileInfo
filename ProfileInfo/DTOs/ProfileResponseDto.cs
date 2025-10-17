using System;

namespace ProfileInfo.DTOs
{
    public class ProfileResponseDto
    {
        public required string Status { get; set; }
        public required UserDto User { get; set; }
        public required string Timestamp { get; set; }
        public required string Fact { get; set; }
    }
}

