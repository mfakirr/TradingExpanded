using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;

namespace TradingExpanded.Models
{
    /// <summary>
    /// Represents an employee working at a wholesale shop
    /// </summary>
    [SaveableClass(Constants.SaveBaseId + 2)]
    public class WholesaleEmployee
    {
        /// <summary>
        /// Possible employee skills
        /// </summary>
        public enum EmployeeSkill
        {
            Management,
            Sales,
            Accounting,
            Security,
            Logistics
        }
        
        [SaveableProperty(1)]
        public string Id { get; set; }
        
        [SaveableProperty(2)]
        public string Name { get; set; }
        
        [SaveableProperty(3)]
        public EmployeeSkill Skill { get; set; }
        
        [SaveableProperty(4)]
        public int SkillLevel { get; set; }
        
        [SaveableProperty(5)]
        public int DailyWage { get; set; }
        
        [SaveableProperty(6)]
        public float Loyalty { get; set; }
        
        [SaveableProperty(7)]
        public CharacterObject Character { get; set; }
        
        /// <summary>
        /// Skill level as a string (Poor, Average, Good, Excellent)
        /// </summary>
        public string SkillLevelString
        {
            get
            {
                if (SkillLevel < 25)
                    return "Poor";
                if (SkillLevel < 50)
                    return "Average";
                if (SkillLevel < 75)
                    return "Good";
                return "Excellent";
            }
        }
        
        /// <summary>
        /// Default constructor for saving/loading
        /// </summary>
        public WholesaleEmployee() { }
        
        /// <summary>
        /// Creates a new employee with the given parameters
        /// </summary>
        public WholesaleEmployee(string name, EmployeeSkill skill, int skillLevel, CharacterObject character = null)
        {
            Id = Constants.GenerateUniqueId();
            Name = name;
            Skill = skill;
            SkillLevel = Math.Clamp(skillLevel, 1, 100);
            DailyWage = CalculateWage();
            Loyalty = 50f; // Neutral loyalty
            Character = character;
        }
        
        /// <summary>
        /// Creates a random employee
        /// </summary>
        public static WholesaleEmployee CreateRandom(Town town = null)
        {
            Random random = new Random();
            
            // Generate random skill
            EmployeeSkill skill = (EmployeeSkill)random.Next(Enum.GetValues(typeof(EmployeeSkill)).Length);
            
            // Generate random skill level (higher chance of lower levels)
            int skillLevel = random.Next(1, 50) + random.Next(0, 51); // 1-100 with bias towards lower values
            
            // Generate random name based on culture if town is provided
            string name;
            CharacterObject character = null;
            
            if (town != null)
            {
                // Get a random character from the town's culture
                var cultureCharacters = town.Settlement.Culture.NotableAndWandererTemplates;
                if (cultureCharacters.Length > 0)
                {
                    character = cultureCharacters[random.Next(cultureCharacters.Length)];
                    name = CharacterObject.PlayerCharacter.Name.ToString(); // Placeholder
                }
                else
                {
                    name = GenerateRandomName(random);
                }
            }
            else
            {
                name = GenerateRandomName(random);
            }
            
            return new WholesaleEmployee(name, skill, skillLevel, character);
        }
        
        /// <summary>
        /// Generates a random name for an employee
        /// </summary>
        private static string GenerateRandomName(Random random)
        {
            string[] maleFirstNames = { "John", "Ali", "Ibrahim", "Mehmet", "Mustafa", "Ahmed", "Hassan", "Omar", "Ahmet", "Kemal" };
            string[] femaleFirstNames = { "Fatima", "Ayşe", "Zeynep", "Leyla", "Elif", "Hatice", "Merve", "Emine", "Nur", "Ayla" };
            string[] lastNames = { "Smith", "Yılmaz", "Demir", "Kaya", "Çelik", "Şahin", "Öztürk", "Aydın", "Yıldız", "Özdemir" };
            
            bool isMale = random.Next(2) == 0;
            string firstName = isMale ? 
                maleFirstNames[random.Next(maleFirstNames.Length)] : 
                femaleFirstNames[random.Next(femaleFirstNames.Length)];
                
            string lastName = lastNames[random.Next(lastNames.Length)];
            
            return $"{firstName} {lastName}";
        }
        
        /// <summary>
        /// Calculates the daily wage based on skill level
        /// </summary>
        public int CalculateWage()
        {
            // Base wage + bonus for skill level
            return 10 + (int)(SkillLevel * 0.5f);
        }
        
        /// <summary>
        /// Gets the benefit provided by this employee based on their skill
        /// </summary>
        public float GetSkillBonus()
        {
            // Return a percentage bonus based on skill level
            return SkillLevel / 100f;
        }
        
        /// <summary>
        /// Updates the employee's loyalty based on wage payments and other factors
        /// </summary>
        public void UpdateLoyalty(bool wagesPaid)
        {
            if (wagesPaid)
            {
                // Increase loyalty when paid
                Loyalty += 1f;
            }
            else
            {
                // Significant decrease when not paid
                Loyalty -= 10f;
            }
            
            // Keep loyalty within bounds
            Loyalty = Math.Clamp(Loyalty, 0f, 100f);
        }
        
        /// <summary>
        /// Checks if the employee might quit based on loyalty
        /// </summary>
        public bool MightQuit()
        {
            Random random = new Random();
            
            // Higher chance of quitting at lower loyalty
            float quitChance = (100f - Loyalty) / 200f; // 0-0.5 based on loyalty
            
            return random.NextDouble() < quitChance;
        }
        
        /// <summary>
        /// Gets the description of what the employee does
        /// </summary>
        public string GetJobDescription()
        {
            switch (Skill)
            {
                case EmployeeSkill.Management:
                    return "Oversees shop operations and improves efficiency.";
                case EmployeeSkill.Sales:
                    return "Negotiates better prices and attracts more customers.";
                case EmployeeSkill.Accounting:
                    return "Keeps track of finances and reduces overhead costs.";
                case EmployeeSkill.Security:
                    return "Protects inventory from theft and damage.";
                case EmployeeSkill.Logistics:
                    return "Optimizes storage and improves inventory management.";
                default:
                    return "Works at the shop.";
            }
        }
        
        /// <summary>
        /// Gets the specific bonus provided by this employee
        /// </summary>
        public string GetBonusDescription()
        {
            float bonus = GetSkillBonus();
            
            switch (Skill)
            {
                case EmployeeSkill.Management:
                    return $"+{(int)(bonus * 10)}% daily income";
                case EmployeeSkill.Sales:
                    return $"+{(int)(bonus * 15)}% selling prices";
                case EmployeeSkill.Accounting:
                    return $"-{(int)(bonus * 10)}% maintenance costs";
                case EmployeeSkill.Security:
                    return $"-{(int)(bonus * 20)}% theft risk";
                case EmployeeSkill.Logistics:
                    return $"+{(int)(bonus * 15)}% storage capacity";
                default:
                    return "No special bonus";
            }
        }
    }
} 