using Novell.Directory.Ldap;
using System;

public class LdapAuthenticationService
{
    private readonly string _ldapHost;
    private readonly int _ldapPort;
    private readonly string _ldapBaseDn;

    public LdapAuthenticationService(string ldapHost, int ldapPort, string ldapBaseDn)
    {
        _ldapHost = ldapHost;
        _ldapPort = ldapPort;
        _ldapBaseDn = ldapBaseDn;
    }

    // Autentica a un usuario contra el servidor LDAP utilizando el nombre de usuario y contraseña proporcionados
    public bool Authenticate(string username, string password)
    {
        try
        {
            var ldapConnection = new LdapConnection();
            // Conectarse al servidor LDAP utilizando el host y puerto especificados
            ldapConnection.Connect(_ldapHost, _ldapPort);

            // Vincular al servidor LDAP utilizando el nombre de usuario y contraseña proporcionados
            ldapConnection.Bind(username, password);

            // Si la operación de vinculación tiene éxito, la autenticación es exitosa
            return true;
            
        }
        catch (LdapException)
        {
            // Si se produce una LdapException, la autenticación falla
            return false;
        }
        catch (Exception ex)
        {
            // Manejar otras excepciones
            throw ex;
        }
    }

    // Obtiene la entrada LDAP del usuario especificado
    public LdapEntry GetUser(string username, string password)
    {
        try
        {
            var ldapConnection = new LdapConnection();
            // Conectarse al servidor LDAP utilizando el host y puerto especificados
            ldapConnection.Connect(_ldapHost, _ldapPort);

            // Vincular al servidor LDAP de forma anónima
            ldapConnection.Bind(username, password);

            // Especificar la base de búsqueda y el filtro para encontrar al usuario
            var searchBase = _ldapBaseDn;
            var searchFilter = $"(sAMAccountName={username})";

            // Realizar la búsqueda LDAP para encontrar al usuario
            var searchResults = ldapConnection.Search(
                searchBase,
                LdapConnection.SCOPE_SUB,
                searchFilter,
                null,
                false
                );

                // Devolver la primera entrada LDAP coincidente (si existe alguna)
                return searchResults.hasMore() ? searchResults.next() : null;
        }
        catch (Exception ex)
        {
            // Manejar excepciones
            throw ex;
        }
    }
}
