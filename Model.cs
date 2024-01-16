using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;

namespace MyApiServer
{
    // Definis la classe Produit
    public class Produit
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public decimal Prix { get; set; }
        public string Date { get; set; }
    }

    // Definie la classe Utilisateur
    public class Utilisateur
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Email { get; set; }
        public string MotDePasse { get; set; }
    }

    // Definie la classe ProduitMiseAJourRequest
    public class ProduitMiseAJourRequest
    {
        public int ProduitId { get; set; }
        public string NouveauNom { get; set; }
        public decimal NouveauPrix { get; set; }
        public string NouvelleDate { get; set; }
    }
}
