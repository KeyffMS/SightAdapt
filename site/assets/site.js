document.documentElement.classList.add("js");
(() => {
  const nav = document.querySelector(".nav-links");
  const host = document.querySelector(".nav");
  if (!nav || !host) return;

  nav.id = "site-navigation";
  const button = document.createElement("button");
  button.type = "button";
  button.className = "menu-toggle";
  button.textContent = "Menu";
  button.setAttribute("aria-controls", nav.id);
  button.setAttribute("aria-expanded", "false");
  host.insertBefore(button, nav);

  const mobile = window.matchMedia("(max-width: 800px)");
  const sync = () => {
    if (!mobile.matches) {
      nav.hidden = false;
      button.setAttribute("aria-expanded", "false");
      return;
    }
    nav.hidden = button.getAttribute("aria-expanded") !== "true";
  };

  button.addEventListener("click", () => {
    const open = button.getAttribute("aria-expanded") === "true";
    button.setAttribute("aria-expanded", String(!open));
    sync();
  });
  mobile.addEventListener?.("change", sync);
  sync();
})();
