namespace AuthenticationApi.Model
{
    public class Permissions
    {
        private readonly Dictionary<Permission, string> _allPermissions;

        public Permissions()
        {
            _allPermissions = new Dictionary<Permission, string>
            {
                { Permission.GetUser, "GetUser" },
                { Permission.AddUser, "AddUser" },
                { Permission.UpdateUser, "UpdateUser" },
                { Permission.DeleteUser, "DeleteUser" },
                { Permission.UpdatePermission, "UpdatePermission" }
            };
        }

        public List<string> GetPermissionNames(List<Permission> permissions)
        {
            return permissions.Select(permissionValue => _allPermissions[permissionValue]).ToList();
        }

        public List<Permission> GetPermissions(List<string> permissions)
        {
            List<Permission> permissionList = new List<Permission>();

            foreach (string permission in permissions)
            {
                List<KeyValuePair<Permission, string>> permissionEnumList = _allPermissions.Where(p => p.Value == permission).Take(1).ToList();

                if (permissionEnumList.Any())
                {
                    permissionList.Add(permissionEnumList[0].Key);
                }
            }

            return permissionList;
        }
    }
}
