using KTXManagement.Demo.Models;
using Microsoft.EntityFrameworkCore;

namespace KTXManagement.Demo.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<TaiKhoan> TaiKhoans => Set<TaiKhoan>();
        public DbSet<SinhVien> SinhViens => Set<SinhVien>();
        public DbSet<QuanLyKTX> QuanLyKTXs => Set<QuanLyKTX>();
        public DbSet<KeToan> KeToans => Set<KeToan>();

        public DbSet<ToaNha> ToaNhas => Set<ToaNha>();
        public DbSet<Tang> Tangs => Set<Tang>();
        public DbSet<Phong> Phongs => Set<Phong>();
        public DbSet<Giuong> Giuongs => Set<Giuong>();

        public DbSet<DangKyO> DangKyOs => Set<DangKyO>();

        public DbSet<PhieuPhi> PhieuPhis => Set<PhieuPhi>();
        public DbSet<ThanhToan> ThanhToans => Set<ThanhToan>();
        public DbSet<CongNo> CongNos => Set<CongNo>();

        public DbSet<PhanAnhSuCo> PhanAnhSuCos => Set<PhanAnhSuCo>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Quan hệ 1-1 TaiKhoan <-> hồ sơ theo vai trò. Dùng Restrict để tránh
            // nhiều đường xoá lồng nhau (multiple cascade paths) trên InMemory/Sqlite/SqlServer.
            modelBuilder.Entity<SinhVien>()
                .HasOne(sv => sv.TaiKhoan)
                .WithOne(tk => tk.HoSoSinhVien)
                .HasForeignKey<SinhVien>(sv => sv.TaiKhoanId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<QuanLyKTX>()
                .HasOne(ql => ql.TaiKhoan)
                .WithOne(tk => tk.HoSoQuanLy)
                .HasForeignKey<QuanLyKTX>(ql => ql.TaiKhoanId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<KeToan>()
                .HasOne(kt => kt.TaiKhoan)
                .WithOne(tk => tk.HoSoKeToan)
                .HasForeignKey<KeToan>(kt => kt.TaiKhoanId)
                .OnDelete(DeleteBehavior.Restrict);

            // Hạ tầng: ToaNha -> Tang -> Phong -> Giuong (cascade để xoá toà nhà kéo theo)
            modelBuilder.Entity<Tang>()
                .HasOne(t => t.ToaNha)
                .WithMany(tn => tn.DanhSachTang)
                .HasForeignKey(t => t.ToaNhaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Phong>()
                .HasOne(p => p.Tang)
                .WithMany(t => t.DanhSachPhong)
                .HasForeignKey(p => p.TangId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Giuong>()
                .HasOne(g => g.Phong)
                .WithMany(p => p.DanhSachGiuong)
                .HasForeignKey(g => g.PhongId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Giuong>()
                .HasOne(g => g.SinhVien)
                .WithMany()
                .HasForeignKey(g => g.SinhVienId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // DangKyO
            modelBuilder.Entity<DangKyO>()
                .HasOne(d => d.SinhVien)
                .WithMany(sv => sv.DanhSachDangKyO)
                .HasForeignKey(d => d.SinhVienId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DangKyO>()
                .HasOne(d => d.Phong)
                .WithMany()
                .HasForeignKey(d => d.PhongId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            modelBuilder.Entity<DangKyO>()
                .HasOne(d => d.Giuong)
                .WithMany()
                .HasForeignKey(d => d.GiuongId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // Tài chính
            modelBuilder.Entity<PhieuPhi>()
                .HasOne(pp => pp.SinhVien)
                .WithMany(sv => sv.DanhSachPhieuPhi)
                .HasForeignKey(pp => pp.SinhVienId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ThanhToan>()
                .HasOne(tt => tt.PhieuPhi)
                .WithMany(pp => pp.DanhSachThanhToan)
                .HasForeignKey(tt => tt.PhieuPhiId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ThanhToan>()
                .HasOne(tt => tt.NguoiThu)
                .WithMany(kt => kt.DanhSachThanhToanDaGhiNhan)
                .HasForeignKey(tt => tt.NguoiThuId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            modelBuilder.Entity<CongNo>()
                .HasOne(cn => cn.SinhVien)
                .WithOne(sv => sv.CongNo)
                .HasForeignKey<CongNo>(cn => cn.SinhVienId)
                .OnDelete(DeleteBehavior.Restrict);

            // Phản ánh / sự cố
            modelBuilder.Entity<PhanAnhSuCo>()
                .HasOne(pa => pa.SinhVien)
                .WithMany(sv => sv.DanhSachPhanAnh)
                .HasForeignKey(pa => pa.SinhVienId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PhanAnhSuCo>()
                .HasOne(pa => pa.Phong)
                .WithMany()
                .HasForeignKey(pa => pa.PhongId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            modelBuilder.Entity<TaiKhoan>()
                .HasIndex(tk => tk.TenDangNhap)
                .IsUnique();
        }
    }
}
