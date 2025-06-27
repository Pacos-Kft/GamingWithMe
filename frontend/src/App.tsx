import { BrowserRouter, Routes, Route } from "react-router-dom";
import './App.css'
import LandingPage from "./pages/LandingPage";

function App() {

  return (
    <>
      <BrowserRouter>
        <Routes>
          <Route>
            <Route index element={<LandingPage />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </>
  )
}

export default App
