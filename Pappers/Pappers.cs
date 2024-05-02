using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Aspectize.Core;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using Papers;

namespace Pappers {
    public interface IEntreprise {

        DataSet GetSirenInfo(string sirenNumber);
        Dictionary<string, object> GetSirenInfo2(string siren);
    }

    public interface IPappers {

        Dictionary<string, object>[] SearchEntrepriseByName(string term, int count);
        Dictionary<string, object>[] SearchEntrepriseByNameOrSiren(string term, int count);
    }

    [Service(Name = "Pappers")]
    public class Pappers : IEntreprise, IPappers { //, IInitializable, ISingleton

        // https://www.pappers.fr/api/documentation

        [Parameter(Name = "ApiToken")]
        string ApiToken = string.Empty;

        const string papersUrl = "https://api.pappers.fr/v2";
        const string papersSuggestionsUrl = "https://suggestions.pappers.fr/v2";

        DataSet IEntreprise.GetSirenInfo(string sirenNumber) {
            throw new NotImplementedException();
        }

        Dictionary<string, object> IEntreprise.GetSirenInfo2(string siren) {

            siren = siren.Replace(" ", string.Empty).Trim();
            if (siren.Length > 9) {
                siren = siren.Substring(0, 9);
            }

            var info = new Dictionary<string, object>();

            if (string.IsNullOrWhiteSpace(siren)) return info;

            // Etreprises : urlPapers/entreprise
            var api_token = ApiToken;

            //var siret = "";
            //var integrer_diffusions_partielles = true;
            //var format_publications_bodacc = "objet"; // ou "texte"

            var champs_supplementaires = "";
            #region Champs supplémentaires disponibles
            // Gratuit : "enseigne_1,enseigne_2,enseigne_3,micro_entreprise,distribution_speciale,code_cedex,libelle_cedex,code_commune,code_region,region,code_departement,departement,distribution_speciale,nomenclature_code_naf,labels";

            // Avec Jeton :  "sites_internet,telephone,email";

            //- `sites_internet` : 1 jeton supplémentaire
            //- `telephone` : 1 jeton supplémentaire *
            //- `email` : 1 jeton supplémentaire *
            //- `labels: orias` : 0.5 jeton supplémentaire
            //- `sanctions` : 1 jeton supplémentaire
            //- `personne_politiquement_exposee` : 1 jeton supplémentaire
            //- `scoring_financier` : 10 jetons supplémentaires
            //- `scoring_non_financier` : 10 jetons supplémentaires

            #endregion
       
            var parametres = new Dictionary<string, object>() { { "api_token", ApiToken }, { "siren", siren }, { "validite_tva_intracommunautaire", true } };

            //parametres.Add("champs_supplementaires", champs_supplementaires);

            var json = AspectizeHttpClient.Get($"{papersUrl}/entreprise", parametres, null);
            dynamic jObj = Newton.Json.JsonConvert.DeserializeObject(json);

            var enCessation = (jObj.date_cessation != null) ? $" - En cessation depuis {jObj.date_cessation.Value}" : string.Empty;

            var nom_entreprise = jObj.nom_entreprise.Value.Trim();

            var personne_morale = jObj.personne_morale.Value;
            var prenom = jObj.prenom.Value;
            var nom = jObj.nom.Value;

            var siege = jObj.siege;

            dynamic e = siege;

            //var etablissements = jObj.etablissements;
            //dynamic lastEtablissement = null;
            //foreach (var et in etablissements) {

            //    if (et.date_cessation != null) continue;
            //    lastEtablissement = et;
            //}
            // e = lastEtablissement;

            var nom_commercial = e.nom_commercial ?? string.Empty;

            var numero = e.numero_voie ?? string.Empty;
            var type_voie = e.type_voie ?? string.Empty;
            var libelle_voie = e.libelle_voie ?? string.Empty;
            var cp = e.code_postal ?? string.Empty;
            var ville = e.ville ?? string.Empty;


            info.Add("Company", $"{nom_entreprise}{enCessation}");
            info.Add("StreetNumber", numero);
            info.Add("Route", $"{type_voie} {libelle_voie}");
            info.Add("Zip", cp);
            info.Add("City", ville);

            int intSiren; var numTVA = "";

            if (int.TryParse(siren, out intSiren)) {
                var cleTVA = (12 + (3 * (intSiren % 97))) % 97;

                numTVA = "FR" + cleTVA.ToString().PadLeft(2, '0') + siren;
            }

            info.Add("TVA", numTVA);

            return info;
        }

        Dictionary<string, object>[] IPappers.SearchEntrepriseByName(string term, int count) {

            if (count == 0) count = 20;

            var parametres = new Dictionary<string, object>() { { "q", term }, { "longueur", count } };

            var json = AspectizeHttpClient.Get(papersSuggestionsUrl, parametres, null);

            var list = new List<Dictionary<string, object>>();

            return list.ToArray();
        }

        static Regex rxIsSiren = new Regex(@"^\d+$");

        Dictionary<string, object>[] IPappers.SearchEntrepriseByNameOrSiren(string term, int count) {

            if (count == 0) count = 20;

            var cibles = "nom_entreprise,siren"; // defaut nom_entreprise: valeurs possibles : nom_entreprise, denomination, nom_complet, representant, siren, siret

            var parametres = new Dictionary<string, object>() { { "q", term }, { "longueur", count }, { "cibles", cibles } };

            var json = AspectizeHttpClient.Get(papersSuggestionsUrl, parametres, null);
            dynamic jObj = Newton.Json.JsonConvert.DeserializeObject(json);

            var info = rxIsSiren.IsMatch(term) ? jObj.resultats_siren : jObj.resultats_nom_entreprise;

            var list = new List<Dictionary<string, object>>();
            foreach (var e in info) {

                var nomEntreprise = e.nom_entreprise.Value;
                var siret = e.siege.siret.Value;
                var cp = e.siege.code_postal.Value;
                var ville = e.siege.ville.Value;

                var label = $"{nomEntreprise} - {cp} {ville}";

                list.Add(AutoCompleteHelper.GetItem(label, siret));
            }


            return list.ToArray();
        }
    }


}
