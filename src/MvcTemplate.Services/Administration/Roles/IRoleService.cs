﻿using MvcTemplate.Objects;
using System;
using System.Linq;

namespace MvcTemplate.Services
{
    public interface IRoleService : IService
    {
        void SeedPermissions(RoleView view);

        IQueryable<RoleView> GetViews();
        RoleView GetView(Int32 id);

        void Create(RoleView view);
        void Edit(RoleView view);
        void Delete(Int32 id);
    }
}
