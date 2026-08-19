namespace KIPL.AssetManagement.Domain.Enums;

/// <summary>Where in a workflow a photo was taken — the prototype captures one at each of these points.</summary>
public enum PhotoKind
{
    /// <summary>Condition of the asset at the moment it was handed out.</summary>
    Assignment = 0,
    /// <summary>Packed goods before dispatch.</summary>
    Dispatch = 1,
    /// <summary>Employee's proof that they received it.</summary>
    ProofOfReceipt = 2,
    /// <summary>Employee's photo when raising a return.</summary>
    ReturnHandover = 3,
    /// <summary>IT's photo during return inspection.</summary>
    ReturnInspection = 4,
    /// <summary>Delivery of an online order, verified by HR.</summary>
    OnlineOrderVerification = 5
}
