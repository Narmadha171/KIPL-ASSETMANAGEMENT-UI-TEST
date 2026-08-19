namespace KIPL.AssetManagement.Domain.Exceptions;

/// <summary>Thrown when an operation would violate an invariant of the domain model.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
