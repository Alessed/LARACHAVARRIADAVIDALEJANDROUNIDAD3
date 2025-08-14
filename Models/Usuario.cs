using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Dentalara.Models
{


    public enum TipoUsuario
    {
        Paciente,    // Valor por defecto
        Dentista,
        Administrador
    }
    public class Usuario
    {
        public int IdUsuario { get; set; }

        
        public string Nombre { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        
        [Required]
        public string Contrasena { get; set; }


        public DateTime FechaRegistro { get; set; }

        public bool Estado { get; set; }


        public string? TokenRecuperacion { get; set; }
        public DateTime? FechaExpiracionToken { get; set; }


        // Nuevo campo para el tipo de usuario
        public TipoUsuario TipoUsuario { get; set; } = TipoUsuario.Paciente; // Valor por defecto

    }
}
