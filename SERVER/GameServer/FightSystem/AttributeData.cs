﻿namespace GameServer.FightSystem
{
    public class AttributeData
    {
        public float Speed { get; set; }
        /// <summary>
        /// life limit
        /// </summary>
        public float MaxHp { get; set; }
        /// <summary>
        /// magic upper limit
        /// </summary>
        public float MaxMp { get; set; }
        /// <summary>
        /// physical attack
        /// </summary>
        public float Ad { get; set; }
        /// <summary>
        /// Magic attack
        /// </summary>
        public float Ap { get; set; }
        /// <summary>
        /// physical defense
        /// </summary>
        public float Def { get; set; }
        /// <summary>
        /// Magic defense
        /// </summary>
        public float Mdef { get; set; }
        /// <summary>
        /// Critical hit rate
        /// </summary>
        public float Cri { get; set; }
        /// <summary>
        /// Critical damage
        /// </summary>
        public float Con { get; set; }
        public float Crd { get; set; }
        /// <summary>
        /// strength
        /// </summary>
        public float Str { get; set; }
        /// <summary>
        /// intelligence
        /// </summary>
        public float Int { get; set; }
        /// <summary>
        /// agile
        /// </summary>
        public float Agi { get; set; }

        /// <summary>
        /// hit rate
        /// </summary>
        public float HitRate { get; set; }
        /// <summary>
        /// Dodge rate
        /// </summary>
        public float DodgeRate { get; set; }

        public AttributeData()
        {
            Reset();
        }

        public void Add(AttributeData other)
        {
            Speed += other.Speed;
            MaxHp += other.MaxHp;
            MaxMp += other.MaxMp;
            Ad += other.Ad;
            Ap += other.Ap;
            Def += other.Def;
            Mdef += other.Mdef;
            Cri += other.Cri;
            Crd += other.Crd;
            Con += other.Con;
            Str += other.Str;
            Int += other.Int;
            Agi += other.Agi;
            HitRate += other.HitRate;
            DodgeRate += other.DodgeRate;
        }

        public void Sub(AttributeData other)
        {
            Speed -= other.Speed;
            MaxHp -= other.MaxHp;
            MaxMp -= other.MaxMp;
            Ad -= other.Ad;
            Ap -= other.Ap;
            Def -= other.Def;
            Mdef -= other.Mdef;
            Cri -= other.Cri;
            Crd -= other.Crd;
            Con -= other.Con;
            Str -= other.Str;
            Int -= other.Int;
            Agi -= other.Agi;
            HitRate -= other.HitRate;
            DodgeRate -= other.DodgeRate;
        }

        public void Reset()
        {
            Speed = 0;
            MaxHp = 0;
            MaxMp = 0;
            Ad = 0;
            Ap = 0;
            Def = 0;
            Mdef = 0;
            Cri = 0;
            Crd = 0;
            Con = 0;
            Str = 0;
            Int = 0;
            Agi = 0;
            HitRate = 0;
            DodgeRate = 0;
        }
    }
}
