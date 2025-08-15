using AutoDocOps.Domain.Interfaces;
using MediatR;

namespace AutoDocOps.Application.Passports.Commands.DeletePassport;

public class DeletePassportHandler : IRequestHandler<DeletePassportCommand, DeletePassportResponse>
{
    private readonly IPassportRepository _passportRepository;

    public DeletePassportHandler(IPassportRepository passportRepository)
    {
        _passportRepository = passportRepository;
    }

    public async Task<DeletePassportResponse> Handle(DeletePassportCommand request, CancellationToken cancellationToken)
    {
    ArgumentNullException.ThrowIfNull(request);

    var success = await _passportRepository.DeleteAsync(request.Id, cancellationToken).ConfigureAwait(false);
        
    return new DeletePassportResponse(
            success, 
            success ? "Passport deleted successfully" : "Passport not found or could not be deleted"
        );
    }
}
