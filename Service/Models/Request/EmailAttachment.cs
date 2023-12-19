namespace AppliedSoftware.Models.Request;

public class EmailAttachment
{
    /// <summary>
    /// Includes the type of document (e.g. .pdf, .docx, .png).
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    public string MimeType { get; set; } = string.Empty;
    
    public byte[] AttachmentBytes { get; set; } = Array.Empty<byte>();
}