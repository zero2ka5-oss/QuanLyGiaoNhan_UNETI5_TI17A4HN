// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Việt hóa các thông báo Validation mặc định của ASP.NET Core MVC.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/// <summary>
/// Thuộc tính kiểu số không nullable (int, decimal...) được MVC tự gắn [Required] với thông báo tiếng Anh.
/// Provider này thay bằng câu tiếng Việt – áp dụng cả kiểm tra phía server lẫn phía trình duyệt.
/// </summary>
public class ThongBaoKiemTraProvider : IValidationMetadataProvider
{
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        foreach (var batBuoc in context.ValidationMetadata.ValidatorMetadata.OfType<RequiredAttribute>())
            if (batBuoc.ErrorMessage is null && batBuoc.ErrorMessageResourceName is null)
                batBuoc.ErrorMessage = "Vui lòng nhập {0}";
    }
}

public static class ThongBaoKiemTra
{
    public static void CauHinh(MvcOptions o)
    {
        o.ModelMetadataDetailsProviders.Add(new ThongBaoKiemTraProvider());
        var tb = o.ModelBindingMessageProvider;
        tb.SetValueMustNotBeNullAccessor(truong => $"Vui lòng nhập {truong}");
        tb.SetMissingBindRequiredValueAccessor(truong => $"Thiếu giá trị cho {truong}");
        tb.SetAttemptedValueIsInvalidAccessor((giaTri, truong) => $"Giá trị '{giaTri}' không hợp lệ cho {truong}");
        tb.SetNonPropertyAttemptedValueIsInvalidAccessor(giaTri => $"Giá trị '{giaTri}' không hợp lệ");
        tb.SetUnknownValueIsInvalidAccessor(truong => $"Giá trị của {truong} không hợp lệ");
        tb.SetValueIsInvalidAccessor(giaTri => $"Giá trị '{giaTri}' không hợp lệ");
        tb.SetValueMustBeANumberAccessor(truong => $"{truong} phải là số");
        tb.SetNonPropertyValueMustBeANumberAccessor(() => "Giá trị phải là số");
    }
}
