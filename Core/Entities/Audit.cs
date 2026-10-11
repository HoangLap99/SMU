using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMU.Core.Entities
{
    /// <summary>
    /// Thực thể cơ sở chứa các trường kiểm toán (Audit Trail) cho toàn bộ các bảng trong cơ sở dữ liệu.
    /// Tự động cập nhật CreatedById, CreatedByName, ModifileById, ModifileByName từ thông tin User của Authentication khi gọi SaveChanges.
    /// </summary>
    public class Audit
    {
        /// <summary>
        /// Khóa chính định danh thực thể (Mã GUID 36 ký tự).
        /// </summary>
        [Key]
        [StringLength(36)]
        public string? Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Thời điểm tạo bản ghi (UTC).
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Mã GUID của người dùng tạo bản ghi (createdById / CreatedById).
        /// </summary>
        [StringLength(36)]
        public string? CreatedById { get; set; }

        /// <summary>
        /// Tên (hoặc Username) của người dùng tạo bản ghi (CreatedByName).
        /// </summary>
        public string? CreatedByName { get; set; }

        /// <summary>
        /// Thời điểm chỉnh sửa bản ghi gần nhất (UTC).
        /// </summary>
        public DateTime Modified { get; set; }

        /// <summary>
        /// Mã GUID của người dùng chỉnh sửa bản ghi gần nhất (ModifileById / ModifiedById).
        /// </summary>
        [StringLength(36)]
        public string? ModifileById { get; set; }

        /// <summary>
        /// Tên (hoặc Username) của người dùng chỉnh sửa bản ghi gần nhất (ModifileByName / ModifiedByName).
        /// </summary>
        public string? ModifileByName { get; set; }

        #region Các trường tương thích ngược (Compatibility Aliases)

        /// <summary>
        /// Alias tương thích cho CreatedById.
        /// </summary>
        [NotMapped]
        public string? CreatedId
        {
            get => CreatedById;
            set => CreatedById = value;
        }

        /// <summary>
        /// Alias tương thích cho CreatedByName.
        /// </summary>
        [NotMapped]
        public string? CreatedName
        {
            get => CreatedByName;
            set => CreatedByName = value;
        }

        /// <summary>
        /// Alias chuẩn ngữ pháp tiếng Anh cho ModifileById (ModifiedById).
        /// </summary>
        [NotMapped]
        public string? ModifiedById
        {
            get => ModifileById;
            set => ModifileById = value;
        }

        /// <summary>
        /// Alias chuẩn ngữ pháp tiếng Anh cho ModifileByName (ModifiedByName).
        /// </summary>
        [NotMapped]
        public string? ModifiedByName
        {
            get => ModifileByName;
            set => ModifileByName = value;
        }

        /// <summary>
        /// Alias tương thích cũ cho ModifiedId.
        /// </summary>
        [NotMapped]
        public string? ModifiedId
        {
            get => ModifileById;
            set => ModifileById = value;
        }

        /// <summary>
        /// Alias tương thích cũ cho ModifiedName.
        /// </summary>
        [NotMapped]
        public string? ModifiedName
        {
            get => ModifileByName;
            set => ModifileByName = value;
        }

        #endregion
    }
}
