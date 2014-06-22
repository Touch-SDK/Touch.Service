using System.Configuration;

namespace Touch.Storage.Configuration
{
    /// <summary>
    /// Storage confguration section.
    /// </summary>
    public class StorageSection : ConfigurationSection
    {
        #region Public
        /// <summary>
        /// Configuration section name.
        /// </summary>
        public const string DefaultSectionName = "system.storageModel/storage";

        static public StorageSection Read(string sectionName) { return (StorageSection)ConfigurationManager.GetSection(sectionName); }

        /// <summary>
        /// Read the section.
        /// </summary>
        /// <returns></returns>
        static public StorageSection Read() { return Read(DefaultSectionName); }
        #endregion

        #region Properties
        /// <summary>
        /// Default file storage.
        /// </summary>
        [ConfigurationProperty("default", IsRequired = true)]
        public StorageSectionItem Default { get { return (StorageSectionItem)this["default"]; } set { this["default"] = value; } }
        #endregion
    }

    /// <summary>
    /// Storage confguration section element.
    /// </summary>
    public class StorageSectionItem : ConfigurationElement
    {
        #region Properties
        /// <summary>
        /// Container name.
        /// </summary>
        [ConfigurationProperty("name", DefaultValue = "", IsRequired = true)]
        public string Name { get { return (string)this["name"]; } set { this["name"] = value; } }

        /// <summary>
        /// Optional folder name.
        /// </summary>
        [ConfigurationProperty("folder", DefaultValue = "", IsRequired = false)]
        public string Folder { get { return (string)this["folder"]; } set { this["folder"] = value; } }

        /// <summary>
        /// Container has read access for anonymous users.
        /// </summary>
        [ConfigurationProperty("public", DefaultValue = true, IsRequired = false)]
        public bool IsPublic { get { return (bool)this["public"]; } set { this["public"] = value; } }

        /// <summary>
        /// Blobs should be stored in reduced redundancy storage.
        /// </summary>
        [ConfigurationProperty("reducedRedundancy", DefaultValue = false, IsRequired = false)]
        public bool ReducedRedundancy { get { return (bool)this["reducedRedundancy"]; } set { this["reducedRedundancy"] = value; } }

        /// <summary>
        /// Domain name, associated with the container.
        /// </summary>
        [ConfigurationProperty("domain", IsRequired = false)]
        public string Domain { get { return this["domain"] as string ?? Name; } set { this["domain"] = value; } }
        #endregion
    }
}
