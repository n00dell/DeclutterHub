using DeclutterHub.Data;
using DeclutterHub.Models;
using Microsoft.EntityFrameworkCore;

namespace DeclutterHub.Services
{
    public class CategoryService
    {
        private readonly DeclutterHubContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CategoryService(DeclutterHubContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task TrackCategoryClick(string userId, int categoryId)
        {
            var click = new CategoryClick
            {
                UserId = userId,
                CategoryId = categoryId,
                ClickedAt = DateTime.UtcNow,
                IpAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                SessionId = _httpContextAccessor.HttpContext?.Session?.Id
            };

            _context.CategoryClick
                .Add(click);
            await _context.SaveChangesAsync();
        }

        // Method to get most clicked categories for a user
        public async Task<List<Category>> GetMostClickedCategories(string userId, int take = 5)
        {
            return await _context.CategoryClick
                .Where(cc => cc.UserId == userId)
                .GroupBy(cc => cc.CategoryId)
                .OrderByDescending(g => g.Count())
                .Take(take)
                .Select(g => g.First().Category)
                .ToListAsync();
        }

        // Method to clean up old click data (optional)
        public async Task CleanupOldClicks(int daysToKeep = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            var oldClicks = await _context.CategoryClick
                .Where(cc => cc.ClickedAt < cutoffDate)
                .ToListAsync();

            _context.CategoryClick.RemoveRange(oldClicks);
            await _context.SaveChangesAsync();
        }
    }
}
