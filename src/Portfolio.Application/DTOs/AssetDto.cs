using Portfolio.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Portfolio.Application.DTOs
{
    public class AssetDto
    {
        public Guid Id { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ExternalId { get; set; }
        public string? ImageUrl { get; set; }
        public string Type { get; set; } = string.Empty;
    }
}
