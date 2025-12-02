using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace appColabDigital
{
    public partial class Form1 : Form
    {
        // Almacenamiento en memoria (para simplicidad)
        private readonly Dictionary<string, User> _users = new();
        private readonly List<AppTask> _tasks = new();
        private readonly List<AppEvent> _events = new();
        private User? _currentUser;

        // Constantes de seguridad
        private const int MaxFailedLoginAttempts = 3;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);

        // Controles de la interfaz de usuario
        private Panel pnlLogin, pnlRegister, pnlMain;
        private TextBox txtLoginUser, txtLoginPassword;
        private TextBox txtRegisterUser, txtRegisterPassword;
        private ListBox lbTasks, lbEvents;
        private TextBox txtTaskDescription, txtEventDescription;
        private DateTimePicker dtpEventDate;

        public Form1()
        {
            InitializeComponent();
            InitializeUI();
            ShowPanel(pnlLogin);
        }

        private void InitializeUI()
        {
            this.Text = "Herramienta de Colaboración Digital";
            this.Size = new Size(800, 600);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Panel de Inicio de Sesión
            pnlLogin = new Panel { Dock = DockStyle.Fill };
            pnlLogin.Controls.AddRange(new Control[]
            {
                new Label { Text = "Iniciar sesión", Font = new Font("Segoe UI", 16, FontStyle.Bold), Location = new Point(300, 30), Size = new Size(200, 30) },
                new Label { Text = "Usuario:", Location = new Point(250, 100) },
                txtLoginUser = new TextBox { Location = new Point(350, 100), Size = new Size(200, 23) },
                new Label { Text = "Contraseña:", Location = new Point(250, 140) },
                txtLoginPassword = new TextBox { Location = new Point(350, 140), Size = new Size(200, 23), UseSystemPasswordChar = true },
                CreateButton("Iniciar Sesión", new Point(350, 180), LoginButton_Click),
                CreateLink("¿No tienes cuenta? Regístrate", new Point(350, 220), (s, e) => ShowPanel(pnlRegister))
            });

            // Panel de Registro
            pnlRegister = new Panel { Dock = DockStyle.Fill, Visible = false };
            pnlRegister.Controls.AddRange(new Control[]
            {
                new Label { Text = "Registro de Usuario", Font = new Font("Segoe UI", 16, FontStyle.Bold), Location = new Point(280, 30), Size = new Size(250, 30) },
                new Label { Text = "Usuario:", Location = new Point(250, 100) },
                txtRegisterUser = new TextBox { Location = new Point(350, 100), Size = new Size(200, 23) },
                new Label { Text = "Contraseña:", Location = new Point(250, 140) },
                txtRegisterPassword = new TextBox { Location = new Point(350, 140), Size = new Size(200, 23), UseSystemPasswordChar = true },
                new Label { Text = "La contraseña debe tener 8+ caracteres, una mayúscula,\nuna minúscula, un número y un símbolo.", Location = new Point(350, 165), Size = new Size(300, 40), ForeColor = Color.Gray },
                CreateButton("Registrar", new Point(350, 210), RegisterButton_Click),
                CreateLink("Volver a Inicio de Sesión", new Point(350, 250), (s, e) => ShowPanel(pnlLogin))
            });

            // Panel Principal
            pnlMain = new Panel { Dock = DockStyle.Fill, Visible = false };
            var lblWelcome = new Label { Text = "Bienvenido", Font = new Font("Segoe UI", 14, FontStyle.Bold), Location = new Point(20, 10), Size = new Size(400, 30) };
            pnlMain.Controls.Add(lblWelcome);

            //Sección de Tareas
            pnlMain.Controls.AddRange(new Control[]
            {
                new Label { Text = "Asignar Tarea", Font = new Font("Segoe UI", 12, FontStyle.Bold), Location = new Point(20, 60) },
                txtTaskDescription = new TextBox { PlaceholderText = "Describe la tarea y menciona a alguien con @usuario...", Location = new Point(20, 90), Size = new Size(350, 23) },
                CreateButton("Asignar", new Point(380, 89), AssignTask_Click),
                new Label { Text = "Tareas Asignadas", Font = new Font("Segoe UI", 10), Location = new Point(20, 130) },
                lbTasks = new ListBox { Location = new Point(20, 150), Size = new Size(740, 150) }
            });

            //Sección de Eventos
            pnlMain.Controls.AddRange(new Control[]
            {
                new Label { Text = "Añadir Evento", Font = new Font("Segoe UI", 12, FontStyle.Bold), Location = new Point(20, 320) },
                txtEventDescription = new TextBox { PlaceholderText = "Descripción del evento...", Location = new Point(20, 350), Size = new Size(350, 23) },
                dtpEventDate = new DateTimePicker { Location = new Point(380, 350), Size = new Size(200, 23), Format = DateTimePickerFormat.Short },
                CreateButton("Añadir", new Point(590, 349), AddEvent_Click),
                new Label { Text = "Próximos Eventos", Font = new Font("Segoe UI", 10), Location = new Point(20, 390) },
                lbEvents = new ListBox { Location = new Point(20, 410), Size = new Size(740, 100) }
            });

            var btnLogout = CreateButton("Cerrar Sesión", new Point(650, 10), LogoutButton_Click);
            btnLogout.BackColor = Color.IndianRed;
            btnLogout.ForeColor = Color.White;
            pnlMain.Controls.Add(btnLogout);

            this.Controls.AddRange(new Control[] { pnlLogin, pnlRegister, pnlMain });
        }

        #region Gestión de Vistas y Controles
        private void ShowPanel(Panel panelToShow)
        {
            pnlLogin.Visible = panelToShow == pnlLogin;
            pnlRegister.Visible = panelToShow == pnlRegister;
            pnlMain.Visible = panelToShow == pnlMain;

            if (panelToShow == pnlMain && _currentUser != null)
            {
                var welcomeLabel = pnlMain.Controls.OfType<Label>().FirstOrDefault(lbl => lbl.Text.StartsWith("Bienvenido"));
                if (welcomeLabel != null) welcomeLabel.Text = $"Bienvenido, {_currentUser.Username}";
                RefreshLists();
            }
        }

        private Button CreateButton(string text, Point location, EventHandler onClick)
        {
            var button = new Button
            {
                Text = text,
                Location = location,
                Size = new Size(120, 30),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += onClick;
            return button;
        }

        private LinkLabel CreateLink(string text, Point location, LinkLabelLinkClickedEventHandler onClick)
        {
            var link = new LinkLabel
            {
                Text = text,
                Location = location,
                AutoSize = true
            };
            link.LinkClicked += onClick;
            return link;
        }
        #endregion

        #region Lógica de Autenticación y Registro
        private void RegisterButton_Click(object? sender, EventArgs e)
        {
            var username = txtRegisterUser.Text.Trim();
            var password = txtRegisterPassword.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("El nombre de usuario y la contraseña no pueden estar vacíos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_users.ContainsKey(username.ToLower()))
            {
                MessageBox.Show("El nombre de usuario ya existe.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!IsValidPassword(password))
            {
                MessageBox.Show("La contraseña no cumple los requisitos de seguridad:\n- Mínimo 8 caracteres\n- Una mayúscula\n- Una minúscula\n- Un número\n- Un símbolo.", "Contraseña Débil", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var salt = GenerateSalt();
            var hashedPassword = HashPassword(password, salt);
            var newUser = new User(username, hashedPassword, salt);
            _users.Add(username.ToLower(), newUser);

            MessageBox.Show("Usuario registrado con éxito.", "Registro Completo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ShowPanel(pnlLogin);
        }

        private void LoginButton_Click(object? sender, EventArgs e)
        {
            var username = txtLoginUser.Text.Trim().ToLower();
            var password = txtLoginPassword.Text;

            if (!_users.TryGetValue(username, out var user))
            {
                MessageBox.Show("Usuario o contraseña incorrectos.", "Error de Inicio de Sesión", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (user.IsLockedOut)
            {
                if (DateTime.UtcNow < user.LockoutEndDate)
                {
                    var minutosRestantes = (user.LockoutEndDate.Value - DateTime.UtcNow).Minutes;
                    MessageBox.Show($"Cuenta bloqueada. Inténtalo de nuevo en {minutosRestantes} minutos.", "Cuenta Bloqueada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                user.ResetLockout();
            }

            var hashedPassword = HashPassword(password, user.Salt);
            if (hashedPassword.SequenceEqual(user.HashedPassword))
            {
                user.ResetLockout();
                _currentUser = user;
                MessageBox.Show($"Inicio de sesión exitoso. ¡Bienvenido, {user.Username}!", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ShowPanel(pnlMain);
                txtLoginUser.Clear();
                txtLoginPassword.Clear();
            }
            else
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= MaxFailedLoginAttempts)
                {
                    user.LockoutEndDate = DateTime.UtcNow.Add(LockoutDuration);
                    MessageBox.Show("Demasiados intentos fallidos. Tu cuenta ha sido bloqueada por 5 minutos.", "Cuenta Bloqueada", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Usuario o contraseña incorrectos.", "Error de Inicio de Sesión", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void LogoutButton_Click(object? sender, EventArgs e)
        {
            _currentUser = null;
            ShowPanel(pnlLogin);
        }
        #endregion

        #region Lógica de la Aplicación Principal
        private void AssignTask_Click(object? sender, EventArgs e)
        {
            if (_currentUser == null) return;

            var description = txtTaskDescription.Text;
            if (string.IsNullOrWhiteSpace(description))
            {
                MessageBox.Show("La descripción de la tarea no puede estar vacía.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var newTask = new AppTask(description, _currentUser.Username);
            _tasks.Add(newTask);
            RefreshLists();
            txtTaskDescription.Clear();
        }

        private void AddEvent_Click(object? sender, EventArgs e)
        {
            if (_currentUser == null) return;

            var description = txtEventDescription.Text;
            var eventDate = dtpEventDate.Value;

            if (string.IsNullOrWhiteSpace(description))
            {
                MessageBox.Show("La descripción del evento no puede estar vacía.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var newEvent = new AppEvent(description, eventDate, _currentUser.Username);
            _events.Add(newEvent);
            RefreshLists();
            txtEventDescription.Clear();
        }

        private void RefreshLists()
        {
            lbTasks.Items.Clear();
            foreach (var task in _tasks.OrderByDescending(t => t.Timestamp))
            {
                lbTasks.Items.Add(task.ToString());
            }

            lbEvents.Items.Clear();
            foreach (var ev in _events.OrderBy(e => e.EventDate))
            {
                lbEvents.Items.Add(ev.ToString());
            }
        }
        #endregion

        #region Utilidades de Seguridad
        private static byte[] GenerateSalt()
        {
            return RandomNumberGenerator.GetBytes(16); // 128 bits
        }

        private static byte[] HashPassword(string password, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(32); // 256 bits
        }

        private static bool IsValidPassword(string password)
        {
            if (password.Length < 8) return false;
            // Regex para validar: al menos una mayúscula, una minúscula, un número y un símbolo.
            return Regex.IsMatch(password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).+$");
        }
        #endregion
    }

    #region Clases de Modelo
    // Clase para almacenar datos del usuario
    public class User
    {
        public string Username { get; }
        public byte[] HashedPassword { get; }
        public byte[] Salt { get; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutEndDate { get; set; }
        public bool IsLockedOut => LockoutEndDate.HasValue && DateTime.UtcNow < LockoutEndDate;

        public User(string username, byte[] hashedPassword, byte[] salt)
        {
            Username = username;
            HashedPassword = hashedPassword;
            Salt = salt;
        }

        public void ResetLockout()
        {
            FailedLoginAttempts = 0;
            LockoutEndDate = null;
        }
    }

    public class AppTask
    {
        public string Description { get; }
        public string AssignedBy { get; }
        public DateTime Timestamp { get; }

        public AppTask(string description, string assignedBy)
        {
            Description = description;
            AssignedBy = assignedBy;
            Timestamp = DateTime.Now;
        }

        public override string ToString()
        {
            // Resalta las menciones para una mejor visualización
            var formattedDescription = Regex.Replace(Description, @"(@\w+)", "-> $1 <--");
            return $"[{Timestamp:g}] Tarea de {AssignedBy}: {formattedDescription}";
        }
    }

    public class AppEvent
    {
        public string Description { get; }
        public DateTime EventDate { get; }
        public string CreatedBy { get; }

        public AppEvent(string description, DateTime eventDate, string createdBy)
        {
            Description = description;
            EventDate = eventDate;
            CreatedBy = createdBy;
        }

        public override string ToString()
        {
            return $"[{EventDate:D}] {Description} (Creado por: {CreatedBy})";
        }
    }
    #endregion
}