// DateTime Helper para obtener fecha y hora del cliente
class DateTimeClientHelper {
    // Obtener fecha y hora actual del cliente en formato local
    static getClientDateTime() {
        const now = new Date();
        // Formatear como yyyy-MM-dd HH:mm:ss (formato local sin timezone)
        const year = now.getFullYear();
        const month = String(now.getMonth() + 1).padStart(2, '0');
        const day = String(now.getDate()).padStart(2, '0');
        const hours = String(now.getHours()).padStart(2, '0');
        const minutes = String(now.getMinutes()).padStart(2, '0');
        const seconds = String(now.getSeconds()).padStart(2, '0');
        
        return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}`;
    }

    // Obtener solo fecha del cliente
    static getClientDate() {
        return new Date().toISOString().split('T')[0];
    }

    // Obtener fecha y hora formateada para mostrar
    static formatDateTime(date = new Date()) {
        return date.toLocaleString('es-GT', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit',
            hour12: false
        });
    }

    // Obtener fecha formateada para mostrar
    static formatDate(date = new Date()) {
        return date.toLocaleDateString('es-GT', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit'
        });
    }

    // Enviar fecha del cliente al servidor (para formularios)
    static addClientDateTimeToForm(formId) {
        const form = document.getElementById(formId);
        if (form) {
            // Agregar campo oculto con fecha y hora del cliente
            let dateTimeField = form.querySelector('input[name="clientDateTime"]');
            if (!dateTimeField) {
                dateTimeField = document.createElement('input');
                dateTimeField.type = 'hidden';
                dateTimeField.name = 'clientDateTime';
                form.appendChild(dateTimeField);
            }
            dateTimeField.value = this.getClientDateTime();
        }
    }

    // Obtener offset del timezone del cliente
    static getTimezoneOffset() {
        return new Date().getTimezoneOffset();
    }

    // Convertir fecha UTC a fecha local del cliente
    static utcToLocal(utcDateString) {
        const date = new Date(utcDateString);
        return date.toLocaleString('es-GT');
    }
}

// Hacer disponible globalmente
window.DateTimeClientHelper = DateTimeClientHelper;
