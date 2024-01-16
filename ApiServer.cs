using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace MyApiServer
{
    internal class ApiServer
    {
        private readonly HttpListener listener;
        private readonly string baseUri;

        private const string DatabaseFileName = "ALL-ARTICLES.db";

        // Constructeur de la classe ApiServer
        public ApiServer(string baseUri)
        {
            this.listener = new HttpListener();
            // Vérifiez si baseUri se termine par '/'
            this.baseUri = baseUri.EndsWith("/") ? baseUri : baseUri + "/";
            listener.Prefixes.Add(baseUri);

            // Initialise la base de données SQLite
            InitializeDatabase();
        }

        // Méthode pour initialiser la base de données SQLite
        private void InitializeDatabase()
        {
            try
            {
                if (!File.Exists(DatabaseFileName))
                {
                    Console.WriteLine("Le fichier de base de données n'existe pas. Création en cours...");

                    SQLiteConnection.CreateFile(DatabaseFileName);

                    using (SQLiteConnection connection = new SQLiteConnection($"Data Source={DatabaseFileName};Version=3;"))
                    {
                        connection.Open();

                        // Crée la table Produits
                        string createTableQuery = "CREATE TABLE Produits (Id INTEGER PRIMARY KEY AUTOINCREMENT, Nom TEXT, Prix DECIMAL, Date TEXT)";
                        using (SQLiteCommand command = new SQLiteCommand(createTableQuery, connection))
                        {
                            command.ExecuteNonQuery();
                        }

                        // Ajout de la table Utilisateurs
                        createTableQuery = "CREATE TABLE Utilisateurs (Id INTEGER PRIMARY KEY AUTOINCREMENT, Nom TEXT, Prenom TEXT, Email TEXT, MotDePasse TEXT)";
                        using (SQLiteCommand command = new SQLiteCommand(createTableQuery, connection))
                        {
                            command.ExecuteNonQuery();
                        }

                        Console.WriteLine("Base de données créée avec succès.");
                    }
                }
                else
                {
                    Console.WriteLine("Le fichier de base de données existe déjà.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'initialisation de la base de données : {ex.Message}");
            }
        }

        // Méthode pour démarrer le serveur
        public void Start()
        {
            try
            {
                listener.Start();
                Console.WriteLine($"Serveur démarré sur {baseUri}");
                listener.BeginGetContext(ListenerCallback, listener);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du démarrage du serveur : {ex.Message}");
            }
        }

        // Méthode pour arrêter le serveur
        public void Stop()
        {
            try
            {
                listener.Stop();
                Console.WriteLine("Serveur arrêté");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'arrêt du serveur : {ex.Message}");
            }
        }

        // Callback du listener qui gère les requêtes
        private void ListenerCallback(IAsyncResult result)
        {
            try
            {
                HttpListenerContext context = listener.EndGetContext(result);
                listener.BeginGetContext(ListenerCallback, listener);
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                Console.WriteLine($"Requête reçue : {request.HttpMethod} {request.Url}");

                if (request.HttpMethod == "GET" && request.Url.LocalPath == "/api/produits/")
                {
                    string responseData = GetProduits();
                    WriteResponse(response, responseData);
                }
                else if (request.HttpMethod == "POST" && request.Url.LocalPath == "/api/produits/ajouter/")
                {
                    string requestData;
                    using (StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        requestData = reader.ReadToEnd();
                    }

                    AjouterProduit(requestData);

                    WriteResponse(response, "Produit ajouté avec succès.");
                }

                else if (request.HttpMethod == "POST" && request.Url.LocalPath == "/api/produits/supprimer/")
                {
                    string requestData;
                    using (StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        requestData = reader.ReadToEnd();
                    }

                    // Ajout de la logique pour supprimer un produit
                    if (int.TryParse(requestData, out int produitId))
                    {
                        SupprimerProduit(produitId);
                        WriteResponse(response, $"Produit avec l'ID {produitId} supprimé avec succès.");
                    }
                    else
                    {
                        response.StatusCode = 400; // Bad Request
                        WriteResponse(response, "Format d'ID de produit invalide.");
                    }
                }


                else if (request.HttpMethod == "POST" && request.Url.LocalPath == "/api/produits/supprimer-tous/")
                {
                    string requestData;
                    using (StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        requestData = reader.ReadToEnd();
                    }

                    // Ajout de la logique pour supprimer tous les produits d'un utilisateur
                    if (int.TryParse(requestData, out int utilisateurId))
                    {
                        SupprimerTousProduitsParId(utilisateurId);
                        WriteResponse(response, $"Tous les produits de l'utilisateur avec l'ID {utilisateurId} ont été supprimés avec succès.");
                    }
                    else
                    {
                        response.StatusCode = 400; // Mauvaise Requête
                        WriteResponse(response, "Format d'ID d'utilisateur invalide.");
                    }

                }


                else if (request.HttpMethod == "POST" && request.Url.LocalPath == "/api/produits/miseajour/")
                {
                    string requestData;
                    using (StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        requestData = reader.ReadToEnd();
                    }

                    // Ajout de la logique pour mettre à jour un produit
                    ProduitMiseAJourRequest produitMiseAJourRequest = JsonConvert.DeserializeObject<ProduitMiseAJourRequest>(requestData);

                    if (produitMiseAJourRequest != null)
                    {
                        MettreAJourProduit(produitMiseAJourRequest.ProduitId, produitMiseAJourRequest.NouveauNom, produitMiseAJourRequest.NouveauPrix, produitMiseAJourRequest.NouvelleDate);
                        WriteResponse(response, $"Produit avec l'ID {produitMiseAJourRequest.ProduitId} mis à jour avec succès.");
                    }
                    else
                    {
                        response.StatusCode = 400; // Mauvaise Requête
                        WriteResponse(response, "Format de données de mise à jour de produit invalide.");
                    }
                }
            
                else
                {
                    response.StatusCode = 404;
                    response.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans le callback : {ex.Message}");
            }
        }

        // Méthode pour écrire la réponse au client
        private void WriteResponse(HttpListenerResponse response, string responseData)
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(responseData);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'écriture de la réponse : {ex.Message}");
            }
        }

        // Méthode pour ajouter un produit
        private void AjouterProduit(string produitData)
        {
            try
            {
                Produit nouveauProduit = JsonConvert.DeserializeObject<Produit>(produitData);

                // Utilisation de données aléatoires pour l'objet Utilisateur
                Utilisateur nouvelUtilisateur = GenerateRandomUser();

                using (SQLiteConnection connection = new SQLiteConnection($"Data Source={DatabaseFileName};Version=3;"))
                {
                    connection.Open();

                    // Insertion dans la table Produits
                    string insertQuery = "INSERT INTO Produits (Nom, Prix, Date) VALUES (@Nom, @Prix, @Date)";
                    using (SQLiteCommand command = new SQLiteCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Nom", nouveauProduit.Nom);
                        command.Parameters.AddWithValue("@Prix", nouveauProduit.Prix);
                        command.Parameters.AddWithValue("@Date", nouveauProduit.Date);
                        command.ExecuteNonQuery();
                    }

                    // Insertion dans la table Utilisateurs avec des données aléatoires
                    string insertUtilisateurQuery = "INSERT INTO Utilisateurs (Nom, Prenom, Email, MotDePasse) VALUES (@Nom, @Prenom, @Email, @MotDePasse)";
                    using (SQLiteCommand command = new SQLiteCommand(insertUtilisateurQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Nom", nouvelUtilisateur.Nom);
                        command.Parameters.AddWithValue("@Prenom", nouvelUtilisateur.Prenom);
                        command.Parameters.AddWithValue("@Email", nouvelUtilisateur.Email);
                        command.Parameters.AddWithValue("@MotDePasse", nouvelUtilisateur.MotDePasse);
                        command.ExecuteNonQuery();
                    }
                }

                Console.WriteLine($"Produit ajouté : {nouveauProduit.Nom} - {nouveauProduit.Prix} - {nouveauProduit.Date}");
                Console.WriteLine($"Utilisateur ajouté : {nouvelUtilisateur.Nom} - {nouvelUtilisateur.Prenom} - {nouvelUtilisateur.Email}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'ajout du produit : {ex.Message}");
            }
        }

        // Méthode pour supprimer un produit
        private static void SupprimerProduit(int produitId)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection($"Data Source={DatabaseFileName};Version=3;"))
                {
                    connection.Open();

                    string deleteQuery = "DELETE FROM Produit WHERE Id = @ProduitId";
                    using (SQLiteCommand command = new SQLiteCommand(deleteQuery, connection))
                    {
                        command.Parameters.AddWithValue("@ProduitId", produitId);
                        command.ExecuteNonQuery();
                    }
                }

                Console.WriteLine($"Produit supprimé avec succès : Id = {produitId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la suppression du produit : {ex.Message}");
            }
        }

        // Méthode pour supprimer un produit a partir de l'ID
        private static void SupprimerTousProduitsParId(int utilisateurId)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection($"Data Source={DatabaseFileName};Version=3;"))
                {
                    connection.Open();

                    // Récupérer l'ID de l'utilisateur dans la table Utilisateurs
                    string selectUserIdQuery = "SELECT Id FROM Utilisateurs WHERE Id = @UtilisateurId";
                    using (SQLiteCommand command = new SQLiteCommand(selectUserIdQuery, connection))
                    {
                        command.Parameters.AddWithValue("@UtilisateurId", utilisateurId);
                        object result = command.ExecuteScalar();

                        if (result != null)
                        {
                            // L'utilisateur existe, maintenant supprimer tous les produits associés
                            string deleteQuery = "DELETE FROM Produits WHERE IdUtilisateur = @UtilisateurId";
                            using (SQLiteCommand deleteCommand = new SQLiteCommand(deleteQuery, connection))
                            {
                                deleteCommand.Parameters.AddWithValue("@UtilisateurId", utilisateurId);
                                deleteCommand.ExecuteNonQuery();
                            }

                            Console.WriteLine($"Tous les produits de l'utilisateur avec l'ID {utilisateurId} ont été supprimés avec succès.");
                        }
                        else
                        {
                            Console.WriteLine($"L'utilisateur avec l'ID {utilisateurId} n'existe pas.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la suppression des produits de l'utilisateur : {ex.Message}");
            }
        }



        // Méthode pour mettre a jour un produit
        private static void MettreAJourProduit(int produitId, string nouveauNom, decimal nouveauPrix, string nouvelleDate)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection($"Data Source={DatabaseFileName};Version=3;"))
                {
                    connection.Open();

                    string updateQuery = "UPDATE Produits SET Nom = @Nom, Prix = @Prix, Date = @Date WHERE Id = @ProduitId";
                    using (SQLiteCommand command = new SQLiteCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Nom", nouveauNom);
                        command.Parameters.AddWithValue("@Prix", nouveauPrix);
                        command.Parameters.AddWithValue("@Date", nouvelleDate);
                        command.Parameters.AddWithValue("@ProduitId", produitId);
                        command.ExecuteNonQuery();
                    }
                }

                Console.WriteLine($"Produit mis à jour avec succès : Id = {produitId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la mise à jour du produit : {ex.Message}");
            }
        }

        // Méthode pour supprimer un utilisateur
        private static void SupprimerUtilisateur(int utilisateurId)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection($"Data Source={DatabaseFileName};Version=3;"))
                {
                    connection.Open();

                    string deleteQuery = "DELETE FROM Utilisateurs WHERE Id = @UtilisateurId";
                    using (SQLiteCommand command = new SQLiteCommand(deleteQuery, connection))
                    {
                        command.Parameters.AddWithValue("@UtilisateurId", utilisateurId);
                        command.ExecuteNonQuery();
                    }
                }

                Console.WriteLine($"Utilisateur supprimé avec succès : Id = {utilisateurId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la suppression de l'utilisateur : {ex.Message}");
            }
        }


        // Méthode pour mettre a jour un utilisateur
        private static void MettreAJourUtilisateur(int utilisateurId, string nouveauNom, string nouveauPrenom, string nouvelEmail, string nouveauMotDePasse)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection($"Data Source={DatabaseFileName};Version=3;"))
                {
                    connection.Open();

                    string updateQuery = "UPDATE Utilisateurs SET Nom = @Nom, Prenom = @Prenom, Email = @Email, MotDePasse = @MotDePasse WHERE Id = @UtilisateurId";
                    using (SQLiteCommand command = new SQLiteCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Nom", nouveauNom);
                        command.Parameters.AddWithValue("@Prenom", nouveauPrenom);
                        command.Parameters.AddWithValue("@Email", nouvelEmail);
                        command.Parameters.AddWithValue("@MotDePasse", nouveauMotDePasse);
                        command.Parameters.AddWithValue("@UtilisateurId", utilisateurId);
                        command.ExecuteNonQuery();
                    }
                }

                Console.WriteLine($"Utilisateur mis à jour avec succès : Id = {utilisateurId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la mise à jour de l'utilisateur : {ex.Message}");
            }
        }


        // Méthode pour générer un utilisateur aléatoire
        private Utilisateur GenerateRandomUser()
        {
            Random random = new Random();
            string[] firstNames = { "John", "Jane", "Alex", "Emily", "Chris", "Sarah", "Michael", "Olivia", "Terence", };
            string[] lastNames = { "Doe", "Smith", "Johnson", "Williams","Soubramanien", "Jones", "Brown", "Davis", "Miller" };
            string[] domains = { "gmail.com", "yahoo.com", "hotmail.com", "outlook.com", "ynov.com" };

            Utilisateur randomUser = new Utilisateur
            {
                Id = random.Next(1000),  // Id aléatoire (à adapter selon votre besoin)
                Nom = lastNames[random.Next(lastNames.Length)],
                Prenom = firstNames[random.Next(firstNames.Length)],
                Email = $"{GenerateRandomString(8)}@{domains[random.Next(domains.Length)]}",
                MotDePasse = GenerateRandomString(10)
            };

            return randomUser;
        }

        // Méthode pour générer une chaîne aléatoire
        private static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            Random random = new Random();
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // Méthode pour récupérer la liste des produits
        private string GetProduits()
        {
            StringBuilder gridBuilder = new StringBuilder();

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection($"Data Source={DatabaseFileName};Version=3;"))
                {
                    connection.Open();

                    string selectQuery = "SELECT Produits.Id, Produits.Nom AS ProduitNom, Produits.Prix, Produits.Date, Utilisateurs.Nom AS UtilisateurNom, Utilisateurs.Prenom, Utilisateurs.Email FROM Produits LEFT JOIN Utilisateurs ON Produits.Id = Utilisateurs.Id";

                    using (SQLiteCommand command = new SQLiteCommand(selectQuery, connection))
                    {
                        using SQLiteDataReader reader = command.ExecuteReader();
                        // Ajouter les en-têtes de colonnes
                        gridBuilder.AppendLine("Id\tProduit\tPrix\tDate\tUtilisateur\tPrenom\tEmail");

                        while (reader.Read())
                        {
                            // Ajouter les données de chaque produit et utilisateur à la grille
                            gridBuilder.AppendLine($"{reader["Id"]}\t{reader["ProduitNom"]}\t{reader["Prix"]}\t{reader["Date"]}\t{reader["UtilisateurNom"]}\t{reader["Prenom"]}\t{reader["Email"]}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la récupération des produits : {ex.Message}");
            }

            return gridBuilder.ToString();
        }

    }
}    