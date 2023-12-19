using AppliedSoftware.Extensions;
using AppliedSoftware.Models.DTOs;

namespace AppliedSoftware.Models.Response.PackageActions;

public class EmailPackageActionResponse : EmailPackageActionDto
{
    public EmailPackageActionResponse(EmailPackageActionDto dto)
    {
        EmailId = dto.EmailId;
        PackageActionId = dto.PackageActionId;
        Recipients = dto.Recipients;
        Sender = dto.Sender;
        Subject = dto.Subject;
        Body = dto.Body;
        EmailTsVector = dto.EmailTsVector;
        CreatedAtUtc = dto.CreatedAtUtc;
        UpdatedAtUtc = dto.UpdatedAtUtc;
        Attachments = dto.Attachments.Select(x => new EmailAttachmentResponse(x)).ToList();
    }
    public new IEnumerable<EmailAttachmentResponse> Attachments { get; set; }
}

public class EmailAttachmentResponse : EmailAttachmentDto
{
    public EmailAttachmentResponse(EmailAttachmentDto emailAttachmentDto)
    {
        AttachmentId = emailAttachmentDto.AttachmentId;
        EmailPackageActionId = emailAttachmentDto.EmailPackageActionId;
        Name = emailAttachmentDto.Name;
        FileType = emailAttachmentDto.FileType;
    }

    public string DownloadUrl => $"/api/v1/download/attachment/{AttachmentId}";
}