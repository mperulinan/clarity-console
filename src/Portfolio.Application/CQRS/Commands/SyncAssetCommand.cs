using MediatR;
using Portfolio.Application.DTOs;

namespace Portfolio.Application.CQRS.Commands;

public record SyncAssetCommand(AssetDto Asset) : IRequest<AssetDto>;
