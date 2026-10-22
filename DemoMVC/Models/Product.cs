using System.ComponentModel.DataAnnotations;

namespace WebBanHang.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        [StringLength(100, ErrorMessage = "Tên sản phẩm tối đa 100 ký tự")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Giá không được để trống")]
        [Range(1000, 1000000000, ErrorMessage = "Giá phải từ 1.000 đến 1 tỷ")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Mô tả không được để trống")]
        [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự")]
        public string Description { get; set; }
    }
}